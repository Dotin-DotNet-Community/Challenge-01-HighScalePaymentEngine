using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Options;
using Microsoft.Extensions.Options;
using System.Runtime.ExceptionServices;

namespace HighScalePaymentEngine.Components;

public sealed class ParallelAuditLogEncryptor : IParallelEncryptor
{
    private readonly EncryptionOptions _options;

    public ParallelAuditLogEncryptor(IOptions<TransactionEngineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.Encryption;
    }

    public IReadOnlyList<EncryptedRecord> Encrypt(IEnumerable<RawAuditLog> logs,
                                                  Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
                                                  int maxDegreeOfParallelism,
                                                  CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logs);
        ArgumentNullException.ThrowIfNull(encryptAlgorithm);
        cancellationToken.ThrowIfCancellationRequested();

        var logList = logs as IReadOnlyList<RawAuditLog> ?? logs.ToList();
        if (logList.Count == 0)
            return Array.Empty<EncryptedRecord>();

        var results = new EncryptedRecord[logList.Count];

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = ResolveParallelism(maxDegreeOfParallelism),
            CancellationToken = cancellationToken
        };

        try
        {
            Parallel.For(0,
                         logList.Count,
                         parallelOptions,
                         i => results[i] = encryptAlgorithm(logList[i]));
        }
        catch (AggregateException aggregate)
            when (aggregate.InnerExceptions.Count == 1)
        {
            ExceptionDispatchInfo
                .Capture(aggregate.InnerExceptions[0])
                .Throw();
        }

        return results;
    }

    private int ResolveParallelism(int requested)
    {
        if (requested > 0 || requested == -1)
            return requested;

        return _options.DefaultMaxDegreeOfParallelism;
    }
}
