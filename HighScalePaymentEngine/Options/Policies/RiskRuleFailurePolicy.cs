namespace HighScalePaymentEngine.Options.Policies;

/// <summary>
/// سیاست رفتار موتور هنگام خطای یک قانون ریسک در فرآیند ارزیابی.
/// </summary>
public enum RiskRuleFailurePolicy
{
    Ignore = 0,
    FailAll = 1
}
