using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Microsoft.Extensions.Logging.Gcp.Internal;

internal static class ChannelExtensions
{
    /// <summary>
    /// Reads all of the data from the channel in batches, enforcing a maximum
    /// interval policy between consuming an item and emitting it in a batch.
    /// </summary>
    internal static IAsyncEnumerable<T[]> ReadAllBatches<T>(
        this ChannelReader<T> source,
        int batchSize,
        TimeSpan timeSpan,
        CancellationToken ct = default
    )
    {
        ArgumentNullException.ThrowIfNull(source);
        if (batchSize < 1) throw new ArgumentOutOfRangeException(nameof(batchSize));
        if (timeSpan < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeSpan));
        return Implementation(ct);

        async IAsyncEnumerable<T[]> Implementation([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var timerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                List<T> buffer = new();
                while (true)
                {
                    var token = buffer.Count == 0 ? cancellationToken : timerCts.Token;
                    (T Value, bool HasValue) item;
                    try
                    {
                        item = (await source.ReadAsync(token).ConfigureAwait(false), true);
                    }
                    catch (ChannelClosedException)
                    {
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        item = default;
                    }

                    if (buffer.Count == 0) timerCts.CancelAfter(timeSpan);
                    if (item.HasValue && item.Value is not null)
                    {
                        buffer.Add(item.Value);
                        if (buffer.Count < batchSize) continue;
                    }

                    yield return buffer.ToArray();
                    buffer.Clear();
                    if (timerCts.TryReset()) continue;
                    timerCts.Dispose();
                    timerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                }

                // Emit what's left before throwing exceptions.
                if (buffer.Count > 0) yield return buffer.ToArray();

                cancellationToken.ThrowIfCancellationRequested();

                // Propagate possible failure of the channel.
                if (source.Completion.IsCompleted) await source.Completion.ConfigureAwait(false);
            }
            finally
            {
                timerCts.Dispose();
            }
        }
    }
}