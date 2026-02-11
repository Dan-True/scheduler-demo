using SchedulerDemo.Scheduler.JobStore;
using SchedulerDemo.Solver.Solver;

namespace SchedulerDemo.DTO
{
    public class ScheduleJobRequestDTO
    {
        public required List<WorkerDTO> Workers { get; set;  }
        public required List<ShiftDTO> Shifts { get; set;  }

        public bool AllowMovePreassigned { get; set;  }

        public Dictionary<string, HashSet<string>> AvailabilityByWorkerId { get; set; }

        public ScheduleJobRequest MapToScheduleJobRequest()
        {
            return new ScheduleJobRequest
            {
                Workers = this.Workers.Select(x => x.MapToWorker()).ToList(),
                Shifts = this.Shifts.Select(x => x.MapToShift()).ToList(),
                AvailabilityByWorkerId = AvailabilityByWorkerId.ToDictionary(
                    kv => kv.Key,
                    kv => (IReadOnlySet<string>)kv.Value // already a HashSet
                ),
                Options = new SolveOptions
                {
                    AllowMovePreassigned = AllowMovePreassigned
                },
                RulesetId = "default" // not used yet
            };
        }
    }
}
