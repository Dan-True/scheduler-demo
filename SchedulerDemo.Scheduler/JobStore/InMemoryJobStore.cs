using SchedulerDemo.Scheduler.JobStore;
using System.Collections.Concurrent;

public sealed class InMemoryJobStore : IJobStore
{
    private readonly ConcurrentDictionary<Guid, JobState> _jobs = new();

    public JobState Create(ScheduleJobRequest request)
    {
        var state = new JobState
        {
            JobId = Guid.NewGuid(),
            Request = request
        };

        if (!_jobs.TryAdd(state.JobId, state))
            throw new InvalidOperationException("Failed to create job.");

        return state;
    }

    public bool TryGet(Guid jobId, out JobState state)
        => _jobs.TryGetValue(jobId, out state!);

    public bool TryCancel(Guid jobId)
    {
        if (!_jobs.TryGetValue(jobId, out var state)) return false;
        state.Cancellation.Cancel();
        return true;
    }

    public bool TryRemove(Guid jobId) => _jobs.TryRemove(jobId, out _);

    public void MarkRunning(Guid jobId)
    {
        var s = Get(jobId);
        s.Status = JobStatus.Running;
        s.StartedAt = DateTimeOffset.UtcNow;
    }

    public void MarkSucceeded(Guid jobId, ScheduleJobResult result)
    {
        var s = Get(jobId);
        s.Status = JobStatus.Succeeded;
        s.Result = result;
        s.FinishedAt = DateTimeOffset.UtcNow;
    }

    public void MarkFailed(Guid jobId, string error)
    {
        var s = Get(jobId);
        s.Status = JobStatus.Failed;
        s.Error = error;
        s.FinishedAt = DateTimeOffset.UtcNow;
    }

    public void MarkCancelled(Guid jobId)
    {
        var s = Get(jobId);
        s.Status = JobStatus.Cancelled;
        s.FinishedAt = DateTimeOffset.UtcNow;
    }

    private JobState Get(Guid jobId)
        => _jobs.TryGetValue(jobId, out var s) ? s : throw new KeyNotFoundException($"Job {jobId} not found.");
}
