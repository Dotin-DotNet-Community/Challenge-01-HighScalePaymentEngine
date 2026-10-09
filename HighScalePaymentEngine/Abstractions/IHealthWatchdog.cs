namespace HighScalePaymentEngine.Abstractions;

public interface IHealthWatchdog : IDisposable
{
    void Start(Action<string> alertLogger, TimeSpan checkInterval);
}
