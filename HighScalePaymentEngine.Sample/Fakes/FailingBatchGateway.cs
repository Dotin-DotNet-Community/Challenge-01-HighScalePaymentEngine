using Microsoft.Extensions.Logging;

namespace HighScalePaymentEngine.Sample.Fakes;

public sealed class FailingBatchGateway
{
    private readonly ILogger _logger;
    private readonly decimal _triggerAmount;

    public FailingBatchGateway(ILogger logger, decimal triggerAmount)
    {
        _logger = logger;
        _triggerAmount = triggerAmount;
    }

    public Task<BatchResult> CallAsync(IReadOnlyList<TransactionRequest> requests,
                                       CancellationToken cancellationToken)
    {
        var amount = requests.Sum(r => r.Amount);

        if (amount == _triggerAmount)
        {
            _logger.LogInformation("  gateway REJECT batch amount={Amount} (trigger)",
                                   amount);
            throw new InvalidOperationException($"Gateway rejected batch with amount {amount}");
        }

        _logger.LogInformation("  gateway OK     batch amount={Amount}",
                               amount);

        return Task.FromResult(new BatchResult(BatchIndex: -1,
                                               TotalItems: requests.Count,
                                               TotalAmount: amount,
                                               IsSuccess: true));
    }
}
