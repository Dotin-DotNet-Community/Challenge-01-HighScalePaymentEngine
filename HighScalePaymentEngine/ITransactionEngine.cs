namespace HighScalePaymentEngine;

public interface ITransactionEngine : IDisposable
{
    void StartHealthWatchdog(Action<string> alertLogger, TimeSpan checkInterval);

    Task<IReadOnlyList<TransactionResult>> DispatchTransactionsWithThrottleAsync(
        IEnumerable<TransactionRequest> requests,
        Func<TransactionRequest, CancellationToken, Task<TransactionResult>> gatewayCaller,
        int maxConcurrentGatewayCalls,
        CancellationToken cancellationToken = default);

    Task<RiskAssessment[]> EvaluateRiskRulesAsync(
        IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
        CancellationToken cancellationToken = default);

    Task<TransactionStatus> GetFastestConfirmationAsync(
        IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
        CancellationToken cancellationToken = default);

    IReadOnlyList<EncryptedRecord> EncryptAuditLogsInParallel(
        IEnumerable<RawAuditLog> logs,
        Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
        int maxDegreeOfParallelism,
        CancellationToken cancellationToken = default);
}

public record TransactionRequest(string Id, decimal Amount);
public record TransactionResult(string Id, bool IsSuccess, string GatewayResponse);
public record RiskAssessment(string RuleName, bool IsApproved, int RiskScore);
public record TransactionStatus(string TransactionId, string State);
public record RawAuditLog(long Id, string Payload);
public record EncryptedRecord(long Id, string EncryptedPayload, int ProcessedByThreadId);
