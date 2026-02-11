using SchedulerDemo.Solver.Models;

namespace SchedulerDemo.Solver.Solver
{
    public class SolveResult
    {
        public bool Success { get; set;  }

        public IReadOnlyList<ShiftAssignment>? Assignments { get; set; }

        public string? FailureReason { get; set; } = null;
    }
}
