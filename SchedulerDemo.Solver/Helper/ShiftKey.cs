using SchedulerDemo.Solver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Helper
{
    // TODO this is not ideal - it abstracts away some complexity in the actual solver, but puts an annoying burden onto the API caller. 
    // redo in the future. No side-effects in any persisted state.
    public static class ShiftKey
    {
        public static string Of(DateOnly date, ShiftType shift)
            => $"{date:yyyy-MM-dd}:{shift}";
    }
}
