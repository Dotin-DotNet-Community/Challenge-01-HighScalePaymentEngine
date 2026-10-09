using Microsoft.Extensions.Options;

namespace HighScalePaymentEngine.Options;

public sealed class TransactionEngineOptionsValidator : IValidateOptions<TransactionEngineOptions>
{
    public ValidateOptionsResult Validate(string? name,
                                          TransactionEngineOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        ValidateHealthWatchdog(options.HealthWatchdog, failures);
        ValidateBatching(options.Batching, failures);
        ValidateRiskEvaluation(options.RiskEvaluation, failures);
        ValidateEncryption(options.Encryption, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateHealthWatchdog(HealthWatchdogOptions options,
                                               List<string> failures)
    {
        if (options.DefaultCheckInterval <= TimeSpan.Zero)
            failures.Add($"{nameof(HealthWatchdogOptions.DefaultCheckInterval)} must be greater than zero.");

        if (options.ShutdownTimeout <= TimeSpan.Zero)
            failures.Add($"{nameof(HealthWatchdogOptions.ShutdownTimeout)} must be greater than zero.");
    }

    private static void ValidateBatching(BatchingOptions options,
                                         List<string> failures)
    {
        if (options.DefaultMaxConcurrency == 0 || options.DefaultMaxConcurrency < -1)
            failures.Add($"{nameof(BatchingOptions.DefaultMaxConcurrency)} must be -1 or a positive integer.");
    }

    private static void ValidateRiskEvaluation(RiskEvaluationOptions options,
                                               List<string> failures)
    {
        //all Enum values is acceptable
    }

    private static void ValidateEncryption(EncryptionOptions options,
                                           List<string> failures)
    {
        if (options.DefaultMaxDegreeOfParallelism == 0 || options.DefaultMaxDegreeOfParallelism < -1)
            failures.Add($"{nameof(EncryptionOptions.DefaultMaxDegreeOfParallelism)} must be -1 or a positive integer.");
    }
}
