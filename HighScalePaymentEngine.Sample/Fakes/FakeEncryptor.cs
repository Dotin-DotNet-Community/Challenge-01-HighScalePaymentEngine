using System.Security.Cryptography;
using System.Text;

namespace HighScalePaymentEngine.Sample.Fakes;

public static class FakeEncryptor
{
    public static Func<RawAuditLog, EncryptedRecord> Create(int workMs)
    {
        return log =>
        {
            var deadline = Environment.TickCount64 + workMs;
            while (Environment.TickCount64 < deadline)
            {
                Thread.SpinWait(10_000);
            }

            var payloadBytes = Encoding.UTF8.GetBytes(log.Payload);
            var hash = SHA256.HashData(payloadBytes);
            var encrypted = Convert.ToHexString(hash)[..16];

            return new EncryptedRecord(Id: log.Id,
                                       EncryptedPayload: encrypted,
                                       ProcessedByThreadId: Environment.CurrentManagedThreadId);
        };
    }
}
