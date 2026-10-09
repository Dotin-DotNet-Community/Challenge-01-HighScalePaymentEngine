using System.Runtime.ExceptionServices;
using HighScalePaymentEngine.Abstractions;
using HighScalePaymentEngine.Models;
using HighScalePaymentEngine.Options;
using HighScalePaymentEngine.Options.Policies;
using Microsoft.Extensions.Options;

namespace HighScalePaymentEngine.Components;

public sealed class ThrottledBatchDispatcher : IBatchDispatcher
{
    private readonly BatchingOptions _options;

    public ThrottledBatchDispatcher(IOptions<TransactionEngineOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value.Batching;
    }

    public async Task<IReadOnlyList<BatchResult>> DispatchAsync(IReadOnlyList<TransactionBatch> batches,
                                                                Func<IReadOnlyList<TransactionRequest>, CancellationToken, Task<BatchResult>> batchGatewayCaller,
                                                                int maxConcurrentCalls,
                                                                CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(batches);
        ArgumentNullException.ThrowIfNull(batchGatewayCaller);

        if (batches.Count == 0)
            return Array.Empty<BatchResult>();

        var effectiveConcurrency = ResolveConcurrency(maxConcurrentCalls);
        var results = new BatchResult[batches.Count];
        var policy = _options.FailurePolicy;

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = effectiveConcurrency,
            CancellationToken = cancellationToken
        };

        try
        {
            await Parallel.ForEachAsync(
                Enumerable.Range(0, batches.Count),
                parallelOptions,
                async (index, ct) =>
                {
                    var batch = batches[index];

                    try
                    {
                        results[index] = await batchGatewayCaller(batch.Requests, ct)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception) when (policy == BatchFailurePolicy.ContinueOnError)
                    {
                        results[index] = new BatchResult(BatchIndex: batch.Index,
                                                         TotalItems: batch.TotalItems,
                                                         TotalAmount: batch.TotalAmount,
                                                         IsSuccess: false);
                    }
                }).ConfigureAwait(false);
        }
        catch (AggregateException aggregate)
            when (aggregate.InnerExceptions.Count == 1)
        {
            ExceptionDispatchInfo
                .Capture(aggregate.InnerExceptions[0])
                .Throw();
        }

        return results;
    }

    private int ResolveConcurrency(int requested)
    {
        if (requested > 0)
            return requested;

        return _options.DefaultMaxConcurrency;
    }
}