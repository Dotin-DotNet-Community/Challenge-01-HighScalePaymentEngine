namespace HighScalePaymentEngine.Options.Policies;

/// <summary>
/// سیاست رفتار موتور هنگام شکست یک دسته در فرآیند ارسال به درگاه پرداخت.
/// </summary>
public enum BatchFailurePolicy
{
    FailFast = 0,
    ContinueOnError = 1
}
