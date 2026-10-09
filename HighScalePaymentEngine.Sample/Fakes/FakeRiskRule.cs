using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace HighScalePaymentEngine.Sample.Fakes;

public static class FakeRiskRule
{
    public static Func<CancellationToken, Task<RiskAssessment>> Approved(ILogger logger,
                                                                         Stopwatch clock,
                                                                         string name,
                                                                         int latencyMs,
                                                                         int riskScore)
    {
        return async ct =>
        {
            logger.LogInformation("  t={EnterMs,5}ms  rule '{Name,-10}' ENTER",
                                  clock.ElapsedMilliseconds,
                                  name);

            await Task.Delay(latencyMs, ct).ConfigureAwait(false);

            logger.LogInformation("  t={ExitMs,5}ms  rule '{Name,-10}' EXIT   approved  score={Score}",
                                  clock.ElapsedMilliseconds,
                                  name,
                                  riskScore);

            return new RiskAssessment(name, IsApproved: true, RiskScore: riskScore);
        };
    }

    public static Func<CancellationToken, Task<RiskAssessment>> Throwing(ILogger logger,
                                                                         Stopwatch clock,
                                                                         string name,
                                                                         int latencyMs,
                                                                         string reason)
    {
        return async ct =>
        {
            logger.LogInformation("  t={EnterMs,5}ms  rule '{Name,-10}' ENTER",
                                  clock.ElapsedMilliseconds,
                                  name);

            await Task.Delay(latencyMs, ct).ConfigureAwait(false);

            logger.LogInformation("  t={ExitMs,5}ms  rule '{Name,-10}' FAIL   {Reason}",
                                  clock.ElapsedMilliseconds,
                                  name,
                                  reason);

            throw new InvalidOperationException(reason);
        };
    }
}
