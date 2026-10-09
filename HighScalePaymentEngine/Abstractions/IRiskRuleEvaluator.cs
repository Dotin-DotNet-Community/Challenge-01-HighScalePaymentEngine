namespace HighScalePaymentEngine.Abstractions;

public interface IRiskRuleEvaluator
{
    Task<IReadOnlyList<RiskAssessment>> EvaluateAsync(IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
                                                      CancellationToken cancellationToken);
}
