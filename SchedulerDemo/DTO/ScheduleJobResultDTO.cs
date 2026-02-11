using SchedulerDemo.Scheduler.JobStore;
using SchedulerDemo.Solver.Models;
using System.Runtime.CompilerServices;

namespace SchedulerDemo.DTO
{
    public class ScheduleJobResultDTO
    {
        public ScheduleJobResultDTO()
        {
        }

        public bool Success { get; set; }

        public IReadOnlyList<AssignmentDTO> Assignments { get; set; }

        public string? Error { get; internal set; }
        public bool Cancelled { get; internal set; }

        public static ScheduleJobResultDTO? MapFromScheduleJobResult(ScheduleJobResult? scheduleJobResult)
        {
            if (scheduleJobResult == null) return null;

            return new ScheduleJobResultDTO
            {
                Assignments = [.. scheduleJobResult.Assignments.Select(a => new AssignmentDTO
                {
                    Date = a.Date,
                    Shift = (ShiftTypeDTO) a.Shift,
                    WorkerIds = a.WorkerIds
                })],
                Success = scheduleJobResult.Success,
                Error = scheduleJobResult.Success ? null : scheduleJobResult.FailureReason
            };
        }
    }
}
