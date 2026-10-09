namespace HighScalePaymentEngine.Abstractions;

public interface INodeSelector
{
    Task<TransactionStatus> SelectFastestAsync(IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
                                               CancellationToken cancellationToken);
}
