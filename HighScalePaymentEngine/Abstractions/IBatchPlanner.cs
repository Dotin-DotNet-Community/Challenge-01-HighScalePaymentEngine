using HighScalePaymentEngine.Models;

namespace HighScalePaymentEngine.Abstractions;

public interface IBatchPlanner
{
    IReadOnlyList<TransactionBatch> Plan(IEnumerable<TransactionRequest> requests,
                                         decimal maxBatchAmount);
}
