using HighScalePaymentEngine;
using HighScalePaymentEngine.DependencyInjection;
using HighScalePaymentEngine.Factories;
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

#region Watchdog
logger.LogInformation("=== Watchdog scenario ===");
logger.LogInformation("Engine starting. Watchdog will report every second for 3 seconds.");

using var engine = factory.Create();

engine.StartHealthWatchdog(alertLogger: message => logger.LogInformation("[watchdog] {Message}", message),
                           checkInterval: TimeSpan.FromMilliseconds(500));

await Task.Delay(TimeSpan.FromSeconds(3));

logger.LogInformation("Disposing engine...");

logger.LogInformation("=== Watchdog scenario finished ===");

#endregion

logger.LogInformation("");

#region Batch + Dispatch
logger.LogInformation("=== Batch + Dispatch scenario ===");

var clock = Stopwatch.StartNew();
var gateway = new FakeBatchGateway(host.Services.GetRequiredService<ILogger<FakeBatchGateway>>(),
                                   clock,
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

using var engine2 = factory.Create();

var results = await engine2.BatchAndDispatchWithThrottleAsync(requests,
                                                              maxBatchAmount: 10m,
                                                              batchGatewayCaller: gateway.CallAsync,
                                                              maxConcurrentGatewayCalls: 2);

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

#endregion