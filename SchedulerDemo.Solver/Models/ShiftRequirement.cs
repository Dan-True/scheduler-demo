using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Models
{

    public enum ShiftType { Morning, Evening, Night }

    public record ShiftRequirement(
        DateOnly Date,
        ShiftType Shift,
        int RequiredWorkers,
        IReadOnlyList<string> PreassignedWorkerIds,
        IReadOnlySet<string> RequiredSkills // placeholder
    );

}
