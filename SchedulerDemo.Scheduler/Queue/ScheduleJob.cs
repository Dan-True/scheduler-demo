using SchedulerDemo.Scheduler.JobStore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Scheduler.Queue
{
    public class ScheduleJob
    {
        public ScheduleJob(Guid guid, ScheduleJobRequest request, CancellationToken cancellationToken)
        {
            JobId = guid;
            Request = request;
            CancellationToken = cancellationToken;
        }

        public Guid JobId { get; set; }

        // TODO this can become quite large, so it should be offloaded to a persistent store and fetched by the worker when processing the job. For simplicity, we'll keep it here for now.
        public ScheduleJobRequest Request { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }
}
