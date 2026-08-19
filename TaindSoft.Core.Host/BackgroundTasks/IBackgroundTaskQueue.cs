namespace TaindSoft.Core.Host.BackgroundTasks
{
    /// <summary>
    /// TODO: Document interface IBackgroundTaskQueue
    /// </summary>
    public interface IBackgroundTaskQueue
    {
        ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem);
        ValueTask<Func<CancellationToken, ValueTask>?> DequeueAsync(CancellationToken cancellationToken);
    }
}
