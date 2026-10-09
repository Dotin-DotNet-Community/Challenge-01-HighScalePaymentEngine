namespace HighScalePaymentEngine.Models;

public sealed record TransactionBatch(int Index, IReadOnlyList<TransactionRequest> Requests)
{
    public int TotalItems => Requests.Count;
    public decimal TotalAmount => Requests.Sum(r => r.Amount);
}
