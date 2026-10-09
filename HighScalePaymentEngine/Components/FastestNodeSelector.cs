using HighScalePaymentEngine.Abstractions;

namespace HighScalePaymentEngine.Components;

public sealed class FastestNodeSelector : INodeSelector
{
    public async Task<TransactionStatus> SelectFastestAsync(IEnumerable<Func<CancellationToken, Task<TransactionStatus>>> nodes,
                                                            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(nodes);
        cancellationToken.ThrowIfCancellationRequested();

        var nodeList = nodes as IReadOnlyList<Func<CancellationToken, Task<TransactionStatus>>>
            ?? nodes.ToList();

        if (nodeList.Count == 0)
        {
            throw new InvalidOperationException("No confirmation nodes were provided.");
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        var pending = new List<Task<TransactionStatus>>(nodeList.Count);
        foreach (var node in nodeList)
        {
            pending.Add(node(linkedCts.Token));
        }

        var failures = new List<Exception>();

        while (pending.Count > 0)
        {
            var completed = await Task.WhenAny(pending).ConfigureAwait(false);
            pending.Remove(completed);

            if (cancellationToken.IsCancellationRequested)
            {
                _ = completed.Exception;
                ObserveFaults(pending);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (completed.IsCompletedSuccessfully)
            {
                linkedCts.Cancel();
                ObserveFaults(pending);
                return completed.Result;
            }

            if (completed.Exception is { } aggregate)
                failures.AddRange(aggregate.InnerExceptions);
        }

        throw new InvalidOperationException(
            "All confirmation nodes failed.",
            failures.Count switch
            {
                0 => null,
                1 => failures[0],
                _ => new AggregateException(failures)
            });
    }

    private static void ObserveFaults(IEnumerable<Task<TransactionStatus>> tasks)
    {
        foreach (var task in tasks)
        {
            _ = task.ContinueWith(static t => _ = t.Exception,
                                  CancellationToken.None,
                                  TaskContinuationOptions.OnlyOnFaulted
                                  | TaskContinuationOptions.ExecuteSynchronously,
                                  TaskScheduler.Default);
        }
    }
}
