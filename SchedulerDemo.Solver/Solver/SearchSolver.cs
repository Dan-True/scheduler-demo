using SchedulerDemo.Solver.Helper;
using SchedulerDemo.Solver.Models;
using SchedulerDemo.Solver.Rules;
using SchedulerDemo.Solver.Skills;
using SchedulerDemo.Solver.Solver.Solver;

namespace SchedulerDemo.Solver.Solver
{
    public sealed class SearchSolver : ISearchSolver
    {
        private readonly IRuleEngine _rules;
        private readonly ISkillMatcher _skills;

        public SearchSolver(IRuleEngine rules, ISkillMatcher skills)
        {
            _rules = rules;
            _skills = skills;
        }

        public Task<SolveResult> SolveAsync(SolveRequest request, CancellationToken token)
        {
            // TODO - this solver has NOT been performance optimized or made data-oriented in any way.
            // It’s a straightforward backtracking search with some very basic heuristics. It’s really just a starting point for experimentation and demonstration.

            // First point for better performance would be to exchange all the lists and reference types for data-oriented structures (arrays, structs, spans, etc) to reduce allocations and GC overhead.
            // Then we can add more advanced heuristics, caching of intermediate results, etc.

            // Build slots to fill
            var slots = BuildSlots(request);

            // Initialize state + apply preassignments
            var state = new ScheduleState();
            if (!ApplyPreassignments(request, slots, state, out var preassignFailure))
                return Task.FromResult(new SolveResult { Success = false, FailureReason = preassignFailure });


            // NOTE: Search and many other of the subsequent helper functions they call take stateful variables and manipulate them, rather than a functional style of pure functions.
            // This is intended and to avoid incessant copying of state - we want to mutate the state as we go and backtrack on it, rather than copying it at each step. This is a common pattern in backtracking search algorithms.
            // BUt it does make the solver hard to e.g. unit-test in parts, and hence it has been tested as a whole.

            // Solve remaining
            var ok = Search(request, slots, state, token);

            if (!ok)
                return Task.FromResult(new SolveResult { Success = false, FailureReason = "No feasible schedule found." });
            
            var shiftAssignments = state.Assigned
                .GroupBy(kv =>
                {
                    // slotId format: yyyy-MM-dd:ShiftType:index
                    var (date, shift) = ParseSlotId(kv.Key);
                    return (date, shift);
                })
                .Select(g => new ShiftAssignment(
                    Date: g.Key.date,
                    Shift: g.Key.shift,
                    WorkerIds: g.Select(x => x.Value).ToList()
                ))
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Shift)
                .ToList();

            return Task.FromResult(new SolveResult { Success = true, Assignments = shiftAssignments });
        }

        private List<Slot> BuildSlots(SolveRequest request)
        {
            var slots = new List<Slot>();
            foreach (var s in request.Shifts)
            {
                for (int i = 0; i < s.RequiredWorkers; i++)
                {
                    slots.Add(new Slot(
                        SlotId: SlotId.Of(s.Date, s.Shift, i),
                        Shift: s
                    ));
                }
            }
            return slots;
        }

        private bool ApplyPreassignments(
            SolveRequest request,
            List<Slot> slots,
            ScheduleState state,
            out string? failure)
        {
            failure = null;

            // If allow move = false, treat preassigned as fixed. If allow move = true, we *could* treat them as soft.
            // For now: if AllowMovePreassigned is true, we simply *don’t lock them in* (placeholder).
            if (request.Options.AllowMovePreassigned)
                return true;

            // Lock preassignments into the first N slots of that shift (simple, deterministic)
            foreach (var slot in slots)
            {
                var pre = slot.Shift.PreassignedWorkerIds;
                if (pre.Count == 0) continue;

                // parse the slot index from slotId to map stable
                var idx = ParseIndex(slot.SlotId);
                if (idx >= pre.Count) continue;

                var workerId = pre[idx];

                // Validate availability
                if (!IsAvailable(request, workerId, slot.Shift))
                {
                    failure = $"Preassigned worker '{workerId}' is not available for slot {slot.SlotId}.";
                    return false;
                }

                // Validate skills (placeholder)
                var worker = request.Workers.FirstOrDefault(w => w.Id == workerId);
                if (worker is null)
                {
                    failure = $"Unknown preassigned worker '{workerId}'.";
                    return false;
                }
                if (!_skills.CanWork(worker, slot.Shift))
                {
                    failure = $"Preassigned worker '{workerId}' lacks skills for {slot.Shift.Date} {slot.Shift.Shift}.";
                    return false;
                }

                // Simple constraint placeholder: no double shift same day
                if (state.IsWorkerAssignedToDate(workerId, slot.Shift.Date))
                {
                    failure = $"Preassignment would double-book worker '{workerId}' on {slot.Shift.Date}.";
                    return false;
                }

                state.Assign(slot.SlotId, workerId);

                if (!_rules.IsValidPartial(state, request.RulesetId))
                {
                    failure = $"Ruleset '{request.RulesetId}' rejected the preassignment for {slot.SlotId}.";
                    return false;
                }
            }

            return true;
        }

        private bool Search(SolveRequest request, List<Slot> slots, ScheduleState state, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // Find next unassigned slot, using MRV (fewest candidates)
            Slot? best = null;
            List<string>? bestCandidates = null;

            foreach (var slot in slots)
            {
                if (state.Assigned.ContainsKey(slot.SlotId))
                    continue;

                var candidates = CandidatesFor(request, state, slot);

                if (candidates.Count == 0)
                    return false; // dead end early

                if (best is null || candidates.Count < bestCandidates!.Count)
                {
                    best = slot;
                    bestCandidates = candidates;

                    if (bestCandidates.Count == 1)
                        break; // can’t get more constrained than this
                }
            }

            // No unassigned slots left -> solved
            if (best is null)
                return true;

            // Try each candidate (could add ordering heuristics later)
            foreach (var workerId in bestCandidates!)
            {
                token.ThrowIfCancellationRequested();

                state.Assign(best.SlotId, workerId);

                if (_rules.IsValidPartial(state, request.RulesetId))
                {
                    // The recursion magic!
                    if (Search(request, slots, state, token))
                        return true;
                }

                state.Unassign(best.SlotId);
            }

            return false;
        }

        private List<string> CandidatesFor(SolveRequest request, ScheduleState state, Slot slot)
        {
            var shift = slot.Shift;

            // Domain: workers who are available + have skills + aren’t already booked that day (placeholder rule)
            var list = new List<string>(capacity: request.Workers.Count);

            foreach (var w in request.Workers)
            {
                if (!IsAvailable(request, w.Id, shift))
                    continue;

                if (!_skills.CanWork(w, shift))
                    continue;

                // Placeholder constraint: no double shift same day
                if (state.IsWorkerAssignedToDate(w.Id, shift.Date))
                    continue;

                list.Add(w.Id);
            }

            return list;
        }

        private bool IsAvailable(SolveRequest request, string workerId, ShiftRequirement shift)
        {
            var key = ShiftKey.Of(shift.Date, shift.Shift);
            return request.AvailabilityByWorkerId.TryGetValue(workerId, out var set) && set.Contains(key);
        }

        private int ParseIndex(string slotId)
        {
            // slotId format: yyyy-MM-dd:ShiftType:index
            var lastColon = slotId.LastIndexOf(':');
            return int.Parse(slotId[(lastColon + 1)..]);
        }

        private (DateOnly date, ShiftType shift) ParseSlotId(string slotId)
        {
            // slotId: yyyy-MM-dd:ShiftType:index
            var parts = slotId.Split(':', 3);
            var date = DateOnly.Parse(parts[0]);
            var shift = Enum.Parse<ShiftType>(parts[1]);
            return (date, shift);
        }

        private sealed record Slot(string SlotId, ShiftRequirement Shift);
    }
}
