using HighScalePaymentEngine.Abstractions;

namespace HighScalePaymentEngine.Factories;

public sealed class TransactionEngineFactory : ITransactionEngineFactory
{
    private readonly IHealthWatchdog _watchdog;
    private readonly IBatchPlanner _planner;
    private readonly IBatchDispatcher _dispatcher;
    private readonly IRiskRuleEvaluator _riskEvaluator;
    private readonly INodeSelector _nodeSelector;
    private readonly IParallelEncryptor _encryptor;

    public TransactionEngineFactory(IHealthWatchdog watchdog,
                                    IBatchPlanner planner,
                                    IBatchDispatcher dispatcher,
                                    IRiskRuleEvaluator riskEvaluator,
                                    INodeSelector nodeSelector,
                                    IParallelEncryptor encryptor)
    {
        ArgumentNullException.ThrowIfNull(watchdog);
        ArgumentNullException.ThrowIfNull(planner);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(riskEvaluator);
        ArgumentNullException.ThrowIfNull(nodeSelector);
        ArgumentNullException.ThrowIfNull(encryptor);

        _watchdog = watchdog;
        _planner = planner;
        _dispatcher = dispatcher;
        _riskEvaluator = riskEvaluator;
        _nodeSelector = nodeSelector;
        _encryptor = encryptor;
    }

    public ITransactionEngine Create() =>
        new TransactionEngine(_watchdog,
                              _planner,
                              _dispatcher,
                              _riskEvaluator,
                              _nodeSelector,
                              _encryptor);
}
