using HighScalePaymentEngine.Models;

namespace HighScalePaymentEngine.Abstractions;

public interface IBatchDispatcher
{
    Task<IReadOnlyList<BatchResult>> DispatchAsync(IReadOnlyList<TransactionBatch> batches,
                                                   Func<IReadOnlyList<TransactionRequest>, CancellationToken, Task<BatchResult>> batchGatewayCaller,
                                                   int maxConcurrentCalls,
                                                   CancellationToken cancellationToken);
}
