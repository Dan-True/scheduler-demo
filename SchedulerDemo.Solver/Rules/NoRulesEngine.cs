using SchedulerDemo.Solver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Rules
{
    public sealed class NoRulesEngine : IRuleEngine
    {
        public bool IsValidPartial(ScheduleState state, string rulesetId) => true;
    }
}
