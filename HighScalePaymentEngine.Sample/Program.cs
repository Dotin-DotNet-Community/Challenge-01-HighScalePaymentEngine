using HighScalePaymentEngine.DependencyInjection;
using HighScalePaymentEngine.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddTransactionEngine(builder.Configuration);

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
var factory = host.Services.GetRequiredService<ITransactionEngineFactory>();

logger.LogInformation("=== Watchdog scenario ===");
logger.LogInformation("Engine starting. Watchdog will report every second for 3 seconds.");

using var engine = factory.Create();

engine.StartHealthWatchdog(alertLogger: message => logger.LogInformation("[watchdog] {Message}", message),
                           checkInterval: TimeSpan.FromMilliseconds(500));

await Task.Delay(TimeSpan.FromSeconds(3));

logger.LogInformation("Disposing engine...");

logger.LogInformation("=== Watchdog scenario finished ===");