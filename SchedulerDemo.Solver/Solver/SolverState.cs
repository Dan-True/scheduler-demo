namespace SchedulerDemo.Solver.Solver
{
    internal class SolverState
    {
        // ShiftId -> WorkerId (or null if unassigned)
        public Dictionary<string, string?> AssignmentByShiftId { get; } = new();

        // For quick conflict checks (simple version: one shift per worker per day)
        public Dictionary<string, HashSet<DateOnly>> WorkedDatesByWorkerId { get; } = new();

        // ShiftId -> current domain (candidate WorkerIds)
        public Dictionary<string, List<string>> DomainByShiftId { get; } = new();

        // Undo stacks
        public Stack<(string shiftId, string? previousWorkerId)> AssignmentUndo { get; } = new();
        public Stack<(string shiftId, string removedWorkerId)> DomainUndo { get; } = new();

        public HashSet<string> FixedAssignments { get; } = new(); // shiftIds that can't be changed

        public bool IsComplete => AssignmentByShiftId.Values.All(v => v != null);
    }
}
