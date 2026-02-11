using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Scheduler.JobStore
{
    public class JobState
    {
        public required Guid JobId { get; init; }
        public required ScheduleJobRequest Request { get; init; }

        public JobStatus Status { get; set; } = JobStatus.Queued;

        public ScheduleJobResult? Result { get; set; }
        public string? Error { get; set; }

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? StartedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }

        // Used for per-job cancellation
        public CancellationTokenSource Cancellation { get; } = new();
    }
}
