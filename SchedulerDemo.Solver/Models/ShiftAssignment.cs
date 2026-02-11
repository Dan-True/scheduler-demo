using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Models
{
    public record ShiftAssignment(DateOnly Date, ShiftType Shift, IReadOnlyList<string> WorkerIds);

}