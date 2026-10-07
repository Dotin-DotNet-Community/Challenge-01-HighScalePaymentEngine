namespace HighScalePaymentEngine;

public interface ITransactionEngine : IDisposable
{
    void StartHealthWatchdog(
        Action<string> alertLogger,
        TimeSpan checkInterval);

    Task<IReadOnlyList<BatchResult>> BatchAndDispatchWithThrottleAsync(
        IEnumerable<TransactionRequest> requests,
        decimal maxBatchAmount,
        Func<IReadOnlyList<TransactionRequest>, CancellationToken, Task<BatchResult>> batchGatewayCaller,
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
public record BatchResult(int BatchIndex, int TotalItems, decimal TotalAmount, bool IsSuccess);
public record RiskAssessment(string RuleName, bool IsApproved, int RiskScore);
public record TransactionStatus(string TransactionId, string State);
public record RawAuditLog(long Id, string Payload);
public record EncryptedRecord(long Id, string EncryptedPayload, int ProcessedByThreadId);