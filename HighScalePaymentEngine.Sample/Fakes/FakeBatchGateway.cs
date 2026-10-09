using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HighScalePaymentEngine.Sample.Fakes;

/// <summary>
/// درگاه پرداخت جعلی با تأخیر مصنوعی برای قابل مشاهده کردن throttle.
/// </summary>
public sealed class FakeBatchGateway
{
    private readonly ILogger<FakeBatchGateway> _logger;
    private readonly Stopwatch _clock;
    private readonly TimeSpan _latency;
    private int _inFlight;
    private int _peakConcurrency;

    public FakeBatchGateway(ILogger<FakeBatchGateway> logger,
                            Stopwatch clock,
                            TimeSpan latency)
    {
        _logger = logger;
        _clock = clock;
        _latency = latency;
    }

    public int PeakConcurrency => _peakConcurrency;

    public async Task<BatchResult> CallAsync(IReadOnlyList<TransactionRequest> requests,
                                             CancellationToken cancellationToken)
    {
        var current = Interlocked.Increment(ref _inFlight);
        UpdatePeak(current);

        var enteredAt = _clock.ElapsedMilliseconds;
        _logger.LogInformation("  t={EnteredMs,5}ms  batch ENTER  items={Items} amount={Amount,8} inFlight={InFlight}",
                               enteredAt,
                               requests.Count,
                               requests.Sum(r => r.Amount),
                               current);

        try
        {
            await Task.Delay(_latency, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _inFlight);
        }

        var exitedAt = _clock.ElapsedMilliseconds;
        _logger.LogInformation("  t={ExitedMs,5}ms  batch EXIT   items={Items} amount={Amount,8} elapsed={Elapsed,5}ms",
                               exitedAt,
                               requests.Count,
                               requests.Sum(r => r.Amount),
                               exitedAt - enteredAt);

        return new BatchResult(BatchIndex: -1,
                               TotalItems: requests.Count,
                               TotalAmount: requests.Sum(r => r.Amount),
                               IsSuccess: true);
    }

    private void UpdatePeak(int current)
    {
        int observed;
        do
        {
            observed = _peakConcurrency;
            if (current <= observed) return;
        }
        while (Interlocked.CompareExchange(ref _peakConcurrency, current, observed) != observed);
    }
}
