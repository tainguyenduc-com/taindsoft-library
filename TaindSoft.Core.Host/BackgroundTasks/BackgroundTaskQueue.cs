using System.Threading.Channels;

namespace TaindSoft.Core.Host.BackgroundTasks
{
    /// <summary>
    /// TODO: Document class BackgroundTaskQueue
    /// </summary>
    public class BackgroundTaskQueue : IBackgroundTaskQueue, IDisposable
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;

        public BackgroundTaskQueue(int capacity = 1000)
        {
            BoundedChannelOptions options = new(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            };
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(options);
        }

        public async ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem)
        {
            ArgumentNullException.ThrowIfNull(workItem);

            await _queue.Writer.WriteAsync(workItem).ConfigureAwait(false);
        }

        public async ValueTask<Func<CancellationToken, ValueTask>?> DequeueAsync(CancellationToken cancellationToken)
        {
            try
            {
                Func<CancellationToken, ValueTask> item = await _queue.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                return item;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
        }

        public void Dispose()
        {
            try { _queue.Writer.Complete(); } catch { }
            GC.SuppressFinalize(this);
        }
    }
}
