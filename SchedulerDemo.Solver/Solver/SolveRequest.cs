using SchedulerDemo.Solver.Models;

namespace SchedulerDemo.Solver.Solver
{
    public class SolveRequest
    {
        public required List<ShiftRequirement> Shifts { get; set; }

        public required IList<Worker> Workers { get; set; }

        public IReadOnlyDictionary<string, IReadOnlySet<string>> AvailabilityByWorkerId { get; set; } = new Dictionary<string, IReadOnlySet<string>>();

        public SolveOptions Options { get; set; }

        public string RulesetId { get; set; }
    }
}
