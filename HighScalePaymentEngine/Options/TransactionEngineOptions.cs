using HighScalePaymentEngine.Options.Policies;

namespace HighScalePaymentEngine.Options;

public sealed class TransactionEngineOptions
{
    public const string SectionName = "TransactionEngine";
    public HealthWatchdogOptions HealthWatchdog { get; set; } = new();
    public BatchingOptions Batching { get; set; } = new();
    public RiskEvaluationOptions RiskEvaluation { get; set; } = new();
    public EncryptionOptions Encryption { get; set; } = new();
}

public sealed class HealthWatchdogOptions
{
    public TimeSpan DefaultCheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public WatchdogDisposeBehavior DisposeBehavior { get; set; } = WatchdogDisposeBehavior.WaitForCompletion;
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public sealed class BatchingOptions
{
    public BatchFailurePolicy FailurePolicy { get; set; } = BatchFailurePolicy.FailFast;
    public int DefaultMaxConcurrency { get; set; } = -1;
}

public sealed class RiskEvaluationOptions
{
    public RiskRuleFailurePolicy FailurePolicy { get; set; } = RiskRuleFailurePolicy.FailAll;
}

public sealed class EncryptionOptions
{
    public int DefaultMaxDegreeOfParallelism { get; set; } = -1;
}
