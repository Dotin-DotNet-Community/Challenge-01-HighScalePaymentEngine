using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Components;
using HighScalePaymentEngine.Factories;
using HighScalePaymentEngine.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace HighScalePaymentEngine.DependencyInjection;

public static class TransactionEngineServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionEngine(this IServiceCollection services,
                                                          Action<TransactionEngineOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);

        var optionsBuilder = services
            .AddOptions<TransactionEngineOptions>()
            .ValidateOnStart();

        if (configure is not null)
            optionsBuilder.Configure(configure);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<TransactionEngineOptions>, TransactionEngineOptionsValidator>());

        services.TryAddSingleton<IHealthWatchdog, HealthWatchdog>();
        services.TryAddSingleton<IBatchPlanner, BestFitDecreasingBatchPlanner>();
        services.TryAddSingleton<IBatchDispatcher, ThrottledBatchDispatcher>();
        services.TryAddSingleton<IRiskRuleEvaluator, ParallelRiskRuleEvaluator>();
        services.TryAddSingleton<INodeSelector, FastestNodeSelector>();
        services.TryAddSingleton<IParallelEncryptor, ParallelAuditLogEncryptor>();

        services.TryAddTransient<ITransactionEngineFactory, TransactionEngineFactory>();

        return services;
    }

    public static IServiceCollection AddTransactionEngine(this IServiceCollection services,
                                                          IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        return services.AddTransactionEngine(options =>
            configuration
                .GetSection(TransactionEngineOptions.SectionName)
                .Bind(options));
    }
}
