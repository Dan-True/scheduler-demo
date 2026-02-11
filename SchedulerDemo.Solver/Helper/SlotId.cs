using SchedulerDemo.Solver.Models;

public static class SlotId
{
    // TODO this is not ideal - it abstracts away some complexity in the actual solver, but puts an annoying burden onto the API caller. 
    // redo in the future. No side-effects in any persisted state.
    public static string Of(DateOnly date, ShiftType shift, int index)
        => $"{date:yyyy-MM-dd}:{shift}:{index}";
}