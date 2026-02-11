using SchedulerDemo.Solver.Models;
using SchedulerDemo.Solver.Solver;

namespace SchedulerDemo.Scheduler.JobStore
{
    public class ScheduleJobRequest
    {
        public required List<Worker> Workers { get; set; }
        public required List<ShiftRequirement> Shifts { get; set; }
        public IReadOnlyDictionary<string, IReadOnlySet<string>> AvailabilityByWorkerId { get; set; }
        public string RulesetId { get; set; }
        public SolveOptions Options { get; set; }
    }
}
