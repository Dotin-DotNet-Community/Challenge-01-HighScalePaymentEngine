namespace HighScalePaymentEngine.Abstractions;

public interface IParallelEncryptor
{
    IReadOnlyList<EncryptedRecord> Encrypt(IEnumerable<RawAuditLog> logs,
                                           Func<RawAuditLog, EncryptedRecord> encryptAlgorithm,
                                           int maxDegreeOfParallelism,
                                           CancellationToken cancellationToken);
}
