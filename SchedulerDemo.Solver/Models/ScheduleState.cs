using System;
using System.Collections.Generic;
using System.Text;

namespace SchedulerDemo.Solver.Models
{
    public sealed class ScheduleState
    {
        // slotId -> workerId
        public Dictionary<string, string> Assigned { get; } = new();

        // workerId -> how many slots assigned (simple constraint placeholder)
        public Dictionary<string, int> Workload { get; } = new();

        public void Assign(string slotId, string workerId)
        {
            Assigned[slotId] = workerId;
            Workload[workerId] = Workload.TryGetValue(workerId, out var n) ? n + 1 : 1;
        }

        public void Unassign(string slotId)
        {
            var workerId = Assigned[slotId];
            Assigned.Remove(slotId);
            Workload[workerId] = Workload[workerId] - 1;
            if (Workload[workerId] == 0) Workload.Remove(workerId);
        }

        public bool IsWorkerAssignedToDate(string workerId, DateOnly date)
        {
            // Placeholder for “max 1 shift per day”, etc. Keep it simple for now.
            // You can replace this with a more efficient index later.
            var prefix = $"{date:yyyy-MM-dd}:";
            return Assigned.Any(kv => kv.Value == workerId && kv.Key.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
