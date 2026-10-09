using HighScalePaymentEngine.Abstractions;

namespace HighScalePaymentEngine;

public sealed class TransactionEngine : ITransactionEngine
{
    private readonly IHealthWatchdog _watchdog;
    private readonly IBatchPlanner _planner;
    private readonly IBatchDispatcher _dispatcher;
    private readonly IRiskRuleEvaluator _riskEvaluator;
    private readonly INodeSelector _nodeSelector;
    private readonly IParallelEncryptor _encryptor;

    private bool _disposed;

    public TransactionEngine(IHealthWatchdog watchdog,
                             IBatchPlanner planner,
                             IBatchDispatcher dispatcher,
                             IRiskRuleEvaluator riskEvaluator,
                             INodeSelector nodeSelector,
                             IParallelEncryptor encryptor)
    {
        ArgumentNullException.ThrowIfNull(watchdog);
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(riskEvaluator);
        ArgumentNullException.ThrowIfNull(nodeSelector);
        ArgumentNullException.ThrowIfNull(encryptor);

        _watchdog = watchdog;
        _planner = planner;
        _dispatcher = dispatcher;
        _riskEvaluator = riskEvaluator;
        _nodeSelector = nodeSelector;
        _encryptor = encryptor;
    }

    public void StartHealthWatchdog(Action<string> alertLogger,
                                    TimeSpan checkInterval)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(alertLogger);

        _watchdog.Start(alertLogger, checkInterval);
    }

    public async Task<IReadOnlyList<BatchResult>> BatchAndDispatchWithThrottleAsync(IEnumerable<TransactionRequest> requests,
                                                                                    decimal maxBatchAmount,
                                                                                    Func<IReadOnlyList<TransactionRequest>, CancellationToken, Task<BatchResult>> batchGatewayCaller,
                                                                                    int maxConcurrentGatewayCalls,
                                                                                    CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(batchGatewayCaller);

        var batches = _planner.Plan(requests, maxBatchAmount);

        return await _dispatcher
            .DispatchAsync(batches,
                           batchGatewayCaller,
                           maxConcurrentGatewayCalls,
                           cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<RiskAssessment[]> EvaluateRiskRulesAsync(IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
                                                               CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(riskRules);

        var results = await _riskEvaluator
            .EvaluateAsync(riskRules, cancellationToken)
            .ConfigureAwait(false);

        return results as RiskAssessment[] ?? results.ToArray();
    }

    public Task<TransactionStatus> GetFastestConfirmationAsync(IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
                                                               CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(nodes);

        return _nodeSelector.SelectFastestAsync(nodes, cancellationToken);
    }

    public IReadOnlyList<EncryptedRecord> EncryptAuditLogsInParallel(IEnumerable<RawAuditLog> logs,
                                                                     Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
                                                                     int maxDegreeOfParallelism,
                                                                     CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentNullException.ThrowIfNull(encryptAlgorithm);

        return _encryptor.Encrypt(logs,
                                  encryptAlgorithm,
                                  maxDegreeOfParallelism,
                                  cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _watchdog.Dispose();
    }
}
