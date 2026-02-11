using SchedulerDemo.Solver.Models;

namespace SchedulerDemo.Solver.Rules
{
    public interface IRuleEngine
    {
        // Called after each tentative assignment. Return false to reject.
        bool IsValidPartial(ScheduleState state, string rulesetId);
    }
}
