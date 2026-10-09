using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Models;

namespace HighScalePaymentEngine.Components;

/// <summary>
/// استراتژی دسته‌بندی تراکنش‌ها با الگوریتم Best Fit Decreasing.
/// تراکنش‌ها نزولی مرتب می‌شوند و هر کدام در دسته‌ای قرار می‌گیرد
/// که کمترین فضای باقی‌مانده را دارد ولی هنوز جا می‌شود.
/// </summary>
/// <remarks>
/// <para>
/// این الگوریتم هم تعداد دسته‌ها را کمینه می‌کند و هم توزیع مبالغ
/// را متوازن نگه می‌دارد؛ یعنی هیچ دسته‌ای خالی‌مانند نمی‌شود و
/// هر دسته مخلوطی از مبالغ بزرگ و کوچک دارد.
/// </para>
/// <para>
/// ترتیب اصلی ورودی حفظ نمی‌شود؛ این یک تصمیم دامنه‌ای آگاهانه است.
/// اگر کسب‌وکار به ترتیب FIFO نیاز داشته باشد، باید از استراتژی دیگری
/// استفاده شود.
/// </para>
/// <para>
/// stateless و thread-safe است.
/// </para>
/// </remarks>
public sealed class BestFitDecreasingBatchPlanner : IBatchPlanner
{
    public IReadOnlyList<TransactionBatch> Plan(IEnumerable<TransactionRequest> requests,
                                                decimal maxBatchAmount)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxBatchAmount);

        var materialized = requests as IReadOnlyCollection<TransactionRequest> ?? requests.ToList();

        if (materialized.Count == 0)
            return Array.Empty<TransactionBatch>();

        foreach (var request in materialized)
        {
            if (request.Amount > maxBatchAmount)
                throw new ArgumentException(
                    $"Transaction '{request.Id}' amount ({request.Amount}) " +
                    $"exceeds max batch amount ({maxBatchAmount}).",
                    nameof(requests));
        }

        var ordered = materialized
            .OrderByDescending(r => r.Amount)
            .ToList();

        var builders = new List<BatchBuilder>();

        foreach (var request in ordered)
        {
            var target = FindBestFit(builders, request.Amount, maxBatchAmount);

            if (target is null)
            {
                target = new BatchBuilder();
                builders.Add(target);
            }

            target.Add(request);
        }

        var result = new TransactionBatch[builders.Count];
        for (int i = 0; i < builders.Count; i++)
        {
            result[i] = new TransactionBatch(i, builders[i].Items);
        }

        return result;
    }

    private static BatchBuilder? FindBestFit(List<BatchBuilder> builders,
                                             decimal amount,
                                             decimal maxBatchAmount)
    {
        BatchBuilder? best = null;
        decimal bestRemaining = decimal.MaxValue;

        foreach (var builder in builders)
        {
            var remaining = maxBatchAmount - builder.Sum;

            if (remaining >= amount && remaining < bestRemaining)
            {
                best = builder;
                bestRemaining = remaining;
            }
        }

        return best;
    }

    private sealed class BatchBuilder
    {
        private readonly List<TransactionRequest> _items = new();
        public IReadOnlyList<TransactionRequest> Items => _items;
        public decimal Sum { get; private set; }
        public void Add(TransactionRequest request)
        {
            _items.Add(request);
            Sum += request.Amount;
        }
    }
}
