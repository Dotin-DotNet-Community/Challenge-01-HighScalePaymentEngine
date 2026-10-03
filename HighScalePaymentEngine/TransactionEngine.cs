namespace HighScalePaymentEngine;

public class TransactionEngine : ITransactionEngine
{
    public void StartHealthWatchdog(Action<string> alertLogger, TimeSpan checkInterval)
    {
        throw new NotImplementedException();
    }

    public Task<IReadOnlyList<TransactionResult>> DispatchTransactionsWithThrottleAsync(
        IEnumerable<TransactionRequest> requests,
        Func<TransactionRequest, CancellationToken, Task<TransactionResult>> gatewayCaller,
        int maxConcurrentGatewayCalls,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<RiskAssessment[]> EvaluateRiskRulesAsync(
        IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<TransactionStatus> GetFastestConfirmationAsync(
        IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public IReadOnlyList<EncryptedRecord> EncryptAuditLogsInParallel(
        IEnumerable<RawAuditLog> logs,
        Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
