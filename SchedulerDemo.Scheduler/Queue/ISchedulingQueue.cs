namespace SchedulerDemo.Scheduler.Queue
{
    public interface ISchedulingQueue
    {
        Task EnqueueAsync(ScheduleJob job, CancellationToken ct);
        IAsyncEnumerable<ScheduleJob> DequeueAllAsync(CancellationToken ct);
    }
}
