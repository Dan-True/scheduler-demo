using SchedulerDemo.Solver.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Skills
{
    public interface ISkillMatcher
    {
        bool CanWork(Worker worker, ShiftRequirement shift);
    }
}
