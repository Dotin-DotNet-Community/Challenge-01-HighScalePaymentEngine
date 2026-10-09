using HighScalePaymentEngine;
using HighScalePaymentEngine.DependencyInjection;
using HighScalePaymentEngine.Factories;
using HighScalePaymentEngine.Options.Policies;
using HighScalePaymentEngine.Sample.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTransactionEngine(builder.Configuration);

using var host = builder.Build();
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var factory = host.Services.GetRequiredService<ITransactionEngineFactory>();

await RunWatchdogScenarioAsync(logger, factory);
await RunBatchDispatchScenarioAsync(logger, factory);

logger.LogInformation("Running with policy = {Policy}",
                      RiskRuleFailurePolicy.Ignore);
await RunRiskEvaluationIgnoreScenarioAsync(logger);

logger.LogInformation("Running with policy = {Policy}",
                      RiskRuleFailurePolicy.FailAll);
await RunRiskEvaluationFailAllScenarioAsync(logger);

await RunNodeSelectionRaceScenarioAsync(logger, factory);
await RunNodeSelectionAllFailScenarioAsync(logger, factory);
return;


#region Scenarios
static async Task RunWatchdogScenarioAsync(ILogger logger,
                                           ITransactionEngineFactory factory)
{
    logger.LogInformation("=== Watchdog scenario ===");

    using var engine = factory.Create();
    engine.StartHealthWatchdog(
        alertLogger: message => logger.LogInformation("[watchdog] {Message}", message),
        checkInterval: TimeSpan.FromMilliseconds(500));

    await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
    logger.LogInformation("Disposing engine...");
    logger.LogInformation("");
}

static async Task RunBatchDispatchScenarioAsync(ILogger logger,
                                                ITransactionEngineFactory factory)
{
    logger.LogInformation("=== Batch + Dispatch scenario ===");

    var clock = Stopwatch.StartNew();
    var gateway = new FakeBatchGateway(logger: logger,
                                       clock: clock,
                                       latency: TimeSpan.FromMilliseconds(400));

    var requests = new[]
    {
        new TransactionRequest("T-01", 9m),
        new TransactionRequest("T-02", 8m),
        new TransactionRequest("T-03", 7m),
        new TransactionRequest("T-04", 3m),
        new TransactionRequest("T-05", 3m),
        new TransactionRequest("T-06", 2m),
        new TransactionRequest("T-07", 2m),
        new TransactionRequest("T-08", 1m),
    };

    using var engine = factory.Create();

    var results = await engine.BatchAndDispatchWithThrottleAsync(requests,
                                                                 maxBatchAmount: 10m,
                                                                 batchGatewayCaller: gateway.CallAsync,
                                                                 maxConcurrentGatewayCalls: 2).ConfigureAwait(false);

    logger.LogInformation("");
    logger.LogInformation("Batch results:");
    foreach (var r in results)
    {
        logger.LogInformation("  batch #{Index}  items={Items}  amount={Amount,6}  success={Success}",
                              r.BatchIndex,
                              r.TotalItems,
                              r.TotalAmount,
                              r.IsSuccess);
    }

    logger.LogInformation("Peak in-flight observed by gateway: {Peak} (limit was 2)",
                          gateway.PeakConcurrency);
    logger.LogInformation("");
}

static async Task RunRiskEvaluationIgnoreScenarioAsync(ILogger logger)
{
    logger.LogInformation("=== Risk Evaluation: Ignore policy ===");

    await using var provider = BuildIsolatedProvider(RiskRuleFailurePolicy.Ignore);
    var factory = provider.GetRequiredService<ITransactionEngineFactory>();

    var clock = Stopwatch.StartNew();
    var rules = BuildRules(logger, clock);

    using var engine = factory.Create();
    var results = await engine.EvaluateRiskRulesAsync(rules).ConfigureAwait(false);

    logger.LogInformation("");
    logger.LogInformation("Returned {Count} of {Total} rules (the failing rule was silently dropped)",
                          results.Length,
                          rules.Length);
    logger.LogInformation("");
}

static async Task RunRiskEvaluationFailAllScenarioAsync(ILogger logger)
{
    logger.LogInformation("=== Risk Evaluation: FailAll policy ===");

    await using var provider = BuildIsolatedProvider(RiskRuleFailurePolicy.FailAll);
    var factory = provider.GetRequiredService<ITransactionEngineFactory>();

    var clock = Stopwatch.StartNew();
    var rules = BuildRules(logger, clock);

    using var engine = factory.Create();

    var stopwatchOuter = Stopwatch.StartNew();

    try
    {
        await engine.EvaluateRiskRulesAsync(rules).ConfigureAwait(false);
        logger.LogInformation("Unexpected: no exception was thrown.");
    }
    catch (InvalidOperationException ex)
    {
        var elapsed = stopwatchOuter.ElapsedMilliseconds;
        logger.LogInformation("");
        logger.LogInformation("Caught expected exception after {Elapsed}ms: {Message}",
                              elapsed,
                              ex.Message);

        await Task.Delay(TimeSpan.FromMilliseconds(500)).ConfigureAwait(false);

        logger.LogInformation("");
        logger.LogInformation("Notice: AML and Velocity continued to run in the background "
                              + "because Task.WhenAll does not cancel its inputs.");
    }

    logger.LogInformation("");
}

static async Task RunNodeSelectionRaceScenarioAsync(ILogger logger,
                                                    ITransactionEngineFactory factory)
{
    logger.LogInformation("=== Node Selection: race with winner ===");

    var clock = Stopwatch.StartNew();
    var nodes = new[]
    {
        FakeNode.Failing   (logger, clock, "node-us-east",  latencyMs: 500, reason: "timeout"),
        FakeNode.Responding(logger, clock, "node-eu-west",  latencyMs: 150, state: "confirmed"),
        FakeNode.Responding(logger, clock, "node-ap-south", latencyMs: 800, state: "confirmed"),
        FakeNode.Failing   (logger, clock, "node-standby",  latencyMs: 300, reason: "unreachable"),
    };

    using var engine = factory.Create();

    var startMs = clock.ElapsedMilliseconds;
    var status = await engine
        .GetFastestConfirmationAsync(nodes)
        .ConfigureAwait(false);
    var elapsed = clock.ElapsedMilliseconds - startMs;

    logger.LogInformation("");
    logger.LogInformation("Winner after {Elapsed}ms: transaction={TxId} state={State}",
                          elapsed,
                          status.TransactionId,
                          status.State);

    await Task.Delay(150).ConfigureAwait(false);
    logger.LogInformation("");
}

static async Task RunNodeSelectionAllFailScenarioAsync(ILogger logger,
                                                       ITransactionEngineFactory factory)
{
    logger.LogInformation("=== Node Selection: all nodes fail ===");

    var clock = Stopwatch.StartNew();
    var nodes = new[]
    {
        FakeNode.Failing(logger, clock, "node-us-east",  latencyMs: 200, reason: "timeout"),
        FakeNode.Failing(logger, clock, "node-eu-west",  latencyMs: 100, reason: "connection refused"),
        FakeNode.Failing(logger, clock, "node-ap-south", latencyMs: 300, reason: "gateway error"),
    };

    using var engine = factory.Create();

    try
    {
        await engine.GetFastestConfirmationAsync(nodes).ConfigureAwait(false);
        logger.LogInformation("Unexpected: no exception was thrown.");
    }
    catch (InvalidOperationException ex)
    {
        logger.LogInformation("");
        logger.LogInformation("Caught expected: {Message}", ex.Message);

        switch (ex.InnerException)
        {
            case AggregateException agg:
                foreach (var inner in agg.InnerExceptions)
                {
                    logger.LogInformation("  inner: {Type}: {Message}",
                                          inner.GetType().Name,
                                          inner.Message);
                }
                break;

            case { } single:
                logger.LogInformation("  inner: {Type}: {Message}",
                                      single.GetType().Name,
                                      single.Message);
                break;
        }
    }

    logger.LogInformation("");
}
#endregion


#region Helpers
static Func<CancellationToken, Task<RiskAssessment>>[] BuildRules(ILogger logger,
                                                                  Stopwatch clock) =>
[
    FakeRiskRule.Approved(logger, clock, "AML",       latencyMs: 300, riskScore: 10),
    FakeRiskRule.Throwing(logger, clock, "Sanctions", latencyMs: 200, reason: "sanctions service unavailable"),
    FakeRiskRule.Approved(logger, clock, "Velocity",  latencyMs: 250, riskScore: 20),
    FakeRiskRule.Approved(logger, clock, "GeoCheck",  latencyMs: 150, riskScore: 5),
];

static ServiceProvider BuildIsolatedProvider(RiskRuleFailurePolicy policy)
{
    var services = new ServiceCollection();
    services.AddLogging(b =>
    {
        b.AddSimpleConsole();
        b.SetMinimumLevel(LogLevel.Information);
    });
    services.AddTransactionEngine(o => o.RiskEvaluation.FailurePolicy = policy);
    return services.BuildServiceProvider();
}
#endregion