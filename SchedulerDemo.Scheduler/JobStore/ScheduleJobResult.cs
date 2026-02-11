using SchedulerDemo.Solver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Scheduler.JobStore
{
    public class ScheduleJobResult
    {
        public bool Success { get; set; }

        public IReadOnlyList<ShiftAssignment> Assignments { get; set; }

        public string? FailureReason { get; set; } = null;
    }
}
