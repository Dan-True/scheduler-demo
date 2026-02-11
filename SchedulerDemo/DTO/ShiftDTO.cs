using SchedulerDemo.Solver.Models;

namespace SchedulerDemo.DTO
{
    public class ShiftDTO
    {
        public DateOnly Date { get; init; }

        public ShiftTypeDTO ShiftType { get; init; }

        public int RequiredWorkers { get; init; } = 1;

        public List<string> PreassignedWorkerIds { get; init; } = new List<string>();
        public HashSet<string> RequiredSkills { get; init; } = new HashSet<string>();

        public ShiftRequirement MapToShift()
        {
            return new ShiftRequirement(Date, (ShiftType) ShiftType, RequiredWorkers, PreassignedWorkerIds.AsReadOnly(), RequiredSkills.AsReadOnly());
        }
    }
}
