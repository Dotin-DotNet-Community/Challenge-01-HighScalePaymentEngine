using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Options;
using HighScalePaymentEngine.Options.Policies;
using Microsoft.Extensions.Options;

namespace HighScalePaymentEngine.Components;

public sealed class ParallelRiskRuleEvaluator : IRiskRuleEvaluator
{
    private readonly RiskEvaluationOptions _options;

    public ParallelRiskRuleEvaluator(IOptions<TransactionEngineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.RiskEvaluation;
    }

    public async Task<IReadOnlyList<RiskAssessment>> EvaluateAsync(IEnumerable<Func<CancellationToken, Task<RiskAssessment>>> riskRules,
                                                                   CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(riskRules);
        cancellationToken.ThrowIfCancellationRequested();

        var rules = riskRules as IReadOnlyList<Func<CancellationToken, Task<RiskAssessment>>>
            ?? riskRules.ToList();

        if (rules.Count == 0)
            return Array.Empty<RiskAssessment>();

        var tasks = new Task<RiskAssessment>[rules.Count];
        for (var i = 0; i < rules.Count; i++)
        {
            tasks[i] = rules[i](cancellationToken);
        }

        if (_options.FailurePolicy == RiskRuleFailurePolicy.FailAll)
            return await Task.WhenAll(tasks).ConfigureAwait(false);

        try
        {
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
        catch
        {

        }

        cancellationToken.ThrowIfCancellationRequested();

        var results = new List<RiskAssessment>(rules.Count);
        foreach (var task in tasks)
        {
            if (task.IsCompletedSuccessfully)
                results.Add(task.Result);
        }

        return results;
    }
}
