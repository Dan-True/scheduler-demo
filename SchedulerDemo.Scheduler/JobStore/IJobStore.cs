namespace SchedulerDemo.Scheduler.JobStore
{
    public interface IJobStore
    {
        JobState Create(ScheduleJobRequest request);

        bool TryGet(Guid jobId, out JobState state);

        bool TryCancel(Guid jobId);  
        bool TryRemove(Guid jobId);

        // Note, these must be changed to return 'Task' if this moves to a real database, but for the in-memory implementation they can be synchronous
        void MarkRunning(Guid jobId);
        void MarkSucceeded(Guid jobId, ScheduleJobResult result);
        void MarkFailed(Guid jobId, string error);
        void MarkCancelled(Guid jobId);
    }
}
