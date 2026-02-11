using SchedulerDemo.Solver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Skills
{
    public class NoSkillMatcher : ISkillMatcher
    {
        public bool CanWork(Worker worker, ShiftRequirement shift) => true;
    }
}
