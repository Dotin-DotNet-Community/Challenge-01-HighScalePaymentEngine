using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Options;
using HighScalePaymentEngine.Options.Policies;
using Microsoft.Extensions.Options;

namespace HighScalePaymentEngine.Components;

public sealed class HealthWatchdog : IHealthWatchdog
{
    private readonly TimeProvider _timeProvider;
    private readonly HealthWatchdogOptions _options;
    private readonly object _sync = new();

    private CancellationTokenSource? _cts;
    private Task? _task;
    private bool _disposed;

    public HealthWatchdog(TimeProvider timeProvider,
                          IOptions<TransactionEngineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(options);

        _timeProvider = timeProvider;
        _options = options.Value.HealthWatchdog;
    }

    public void Start(Action<string> alertLogger, TimeSpan checkInterval)
    {
        ArgumentNullException.ThrowIfNull(alertLogger);

        var interval = checkInterval > TimeSpan.Zero
            ? checkInterval
            : _options.DefaultCheckInterval;

        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_task is { IsCompleted: false })
                throw new InvalidOperationException("Health watchdog is already running.");

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            _task = Task.Run(() => RunAsync(alertLogger, interval, token),
                             CancellationToken.None);
        }
    }

    private async Task RunAsync(Action<string> alertLogger,
                                TimeSpan interval,
                                CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(interval, _timeProvider, token)
                    .ConfigureAwait(false);

                alertLogger($"Health check at {_timeProvider.GetUtcNow():O}");
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {

        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cts;
        Task? task;

        lock (_sync)
        {
            if (_disposed) return;
            _disposed = true;

            cts = _cts;
            task = _task;
            _cts = null;
            _task = null;
        }

        if (cts is null) return;

        try
        {
            cts.Cancel();
        }
        catch (ObjectDisposedException)
        {

        }

        if (_options.DisposeBehavior == WatchdogDisposeBehavior.WaitForCompletion
            && task is not null)
        {
            try
            {
                task.Wait(_options.ShutdownTimeout);
            }
            catch (AggregateException)
            {

            }
        }

        cts.Dispose();
    }
}
