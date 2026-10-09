using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HighScalePaymentEngine.Sample.Fakes;

public static class FakeNode
{
    public static Func<CancellationToken, Task<TransactionStatus>> Responding(ILogger logger,
                                                                              Stopwatch clock,
                                                                              string name,
                                                                              int latencyMs,
                                                                              string state)
    {
        return async ct =>
        {
            logger.LogInformation("  t={EnterMs,5}ms  node '{Name,-14}' REQUEST",
                                  clock.ElapsedMilliseconds,
                                  name);

            try
            {
                await Task.Delay(latencyMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("  t={CancelMs,5}ms  node '{Name,-14}' CANCELED",
                                      clock.ElapsedMilliseconds,
                                      name);
                throw;
            }

            logger.LogInformation("  t={ExitMs,5}ms  node '{Name,-14}' RESPOND  state={State}",
                                  clock.ElapsedMilliseconds,
                                  name,
                                  state);

            return new TransactionStatus($"TX-{name}", state);
        };
    }

    public static Func<CancellationToken, Task<TransactionStatus>> Failing(ILogger logger,
                                                                           Stopwatch clock,
                                                                           string name,
                                                                           int latencyMs,
                                                                           string reason)
    {
        return async ct =>
        {
            logger.LogInformation("  t={EnterMs,5}ms  node '{Name,-14}' REQUEST",
                                  clock.ElapsedMilliseconds,
                                  name);

            try
            {
                await Task.Delay(latencyMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("  t={CancelMs,5}ms  node '{Name,-14}' CANCELED",
                                      clock.ElapsedMilliseconds,
                                      name);
                throw;
            }

            logger.LogInformation("  t={ExitMs,5}ms  node '{Name,-14}' FAIL     {Reason}",
                                  clock.ElapsedMilliseconds,
                                  name,
                                  reason);

            throw new HttpRequestException(reason);
        };
    }
}
