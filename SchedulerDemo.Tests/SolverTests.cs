using SchedulerDemo.Solver.Helper;
using SchedulerDemo.Solver.Models;
using SchedulerDemo.Solver.Rules;
using SchedulerDemo.Solver.Skills;
using SchedulerDemo.Solver.Solver;
using Xunit;

public class SolverTests
{
    private static SearchSolver CreateSolver()
        => new SearchSolver(new NoRulesEngine(), new NoSkillMatcher());

    private static Worker[] MakeWorkers(int count)
        => Enumerable.Range(1, count)
            .Select(i => new Worker($"w{i}", $"Worker {i}", new HashSet<string>()))
            .ToArray();

    private static ShiftRequirement MakeShift(DateOnly date, ShiftType shiftType, int requiredWorkers, params string[] preassigned)
        => new ShiftRequirement(
            Date: date,
            Shift: shiftType,
            RequiredWorkers: requiredWorkers,
            PreassignedWorkerIds: preassigned,
            RequiredSkills: new HashSet<string>()
        );

    private static void AddAvailability(
        Dictionary<string, IReadOnlySet<string>> availability,
        string workerId,
        IEnumerable<string> shiftKeys)
    {
        availability[workerId] = new HashSet<string>(shiftKeys);
    }

    private static SolveRequest MakeRequest(
        IEnumerable<ShiftRequirement> shifts,
        IEnumerable<Worker> workers,
        Dictionary<string, IReadOnlySet<string>> availability,
        bool allowMovePreassigned = false,
        string rulesetId = "default")
        => new SolveRequest
        {
            Shifts = shifts.ToList(),
            Workers = workers.ToArray(),
            AvailabilityByWorkerId = availability,
            Options = new SolveOptions { AllowMovePreassigned = allowMovePreassigned },
            RulesetId = rulesetId
        };

    private static void AssertAllAssignmentsAreValid(SolveResult result)
    {
        Assert.True(result.Success);
        Assert.NotNull(result.Assignments);

        // Basic sanity: each assignment should have at least one worker and no duplicates in a shift
        foreach (var a in result.Assignments)
        {
            Assert.NotEmpty(a.WorkerIds);
            Assert.Equal(a.WorkerIds.Count, a.WorkerIds.Distinct().Count());
        }
    }

    private static void AssertNoWorkerDoubleBookedSameDay(SolveResult result)
    {
        var used = new HashSet<(DateOnly date, string workerId)>();

        foreach (var a in result.Assignments)
        {
            foreach (var wid in a.WorkerIds)
            {
                Assert.True(used.Add((a.Date, wid)), $"Worker {wid} double-booked on {a.Date}.");
            }
        }
    }

    [Fact]
    public async Task SolveAsync_LargeSolvable_14Days_2ShiftsPerDay_12Workers_Completes()
    {
        var solver = CreateSolver();

        var start = new DateOnly(2026, 02, 01);
        var days = 14;
        var workers = MakeWorkers(12);

        // 14 days * 2 shifts/day, each requires 1 worker => 28 assignments
        var shifts = new List<ShiftRequirement>();
        for (int d = 0; d < days; d++)
        {
            var date = start.AddDays(d);
            shifts.Add(MakeShift(date, ShiftType.Morning, requiredWorkers: 1));
            shifts.Add(MakeShift(date, ShiftType.Evening, requiredWorkers: 1));
        }

        // Availability: each worker can do all Morning+Evening across all days
        // (Solver also enforces no double shift per day via state.IsWorkerAssignedToDate placeholder)
        var availability = new Dictionary<string, IReadOnlySet<string>>();
        var allShiftKeys = shifts.Select(s => ShiftKey.Of(s.Date, s.Shift)).Distinct();

        foreach (var w in workers)
            AddAvailability(availability, w.Id, allShiftKeys);

        var request = MakeRequest(shifts, workers, availability);

        var result = await solver.SolveAsync(request, CancellationToken.None);

        AssertAllAssignmentsAreValid(result);
        Assert.Equal(28, result.Assignments.Count);
        AssertNoWorkerDoubleBookedSameDay(result);
    }

    [Fact]
    public async Task SolveAsync_Unsolvable_WhenDemandExceedsDailyCapacity_ReturnsFailure()
    {
        var solver = CreateSolver();

        var start = new DateOnly(2026, 02, 01);
        var days = 14;

        // Only 7 workers total, but every day requires Morning+Evening (2 workers/day),
        // and we will artificially make only ONE worker available per day -> impossible due to no-double-booking/day.
        var workers = MakeWorkers(7);

        var shifts = new List<ShiftRequirement>();
        for (int d = 0; d < days; d++)
        {
            var date = start.AddDays(d);
            shifts.Add(MakeShift(date, ShiftType.Morning, 1));
            shifts.Add(MakeShift(date, ShiftType.Evening, 1));
        }

        // Availability: for each date, only worker w1 is available for both shifts (but solver forbids double-booked/day)
        var availability = new Dictionary<string, IReadOnlySet<string>>();
        var w1 = workers[0].Id;

        var w1Keys = shifts.Select(s => ShiftKey.Of(s.Date, s.Shift));
        AddAvailability(availability, w1, w1Keys);

        // Everyone else available for nothing
        for (int i = 1; i < workers.Length; i++)
            AddAvailability(availability, workers[i].Id, Array.Empty<string>());

        var request = MakeRequest(shifts, workers, availability);

        var result = await solver.SolveAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.FailureReason);
    }

    [Fact]
    public async Task SolveAsync_Unsolvable_WhenPreassignedWorkerNotAvailable_ReturnsFailure()
    {
        var solver = CreateSolver();

        var date = new DateOnly(2026, 02, 11);

        var w1 = new Worker("w1", "Alice", new HashSet<string>());
        var w2 = new Worker("w2", "Bob", new HashSet<string>());
        var workers = new[] { w1, w2 };

        // Preassign w1 to Morning, but w1 is NOT available -> must fail during ApplyPreassignments
        var shifts = new[]
        {
            MakeShift(date, ShiftType.Morning, requiredWorkers: 1, preassigned: "w1")
        };

        var availability = new Dictionary<string, IReadOnlySet<string>>
        {
            ["w1"] = new HashSet<string>(), // not available
            ["w2"] = new HashSet<string> { ShiftKey.Of(date, ShiftType.Morning) }
        };

        var request = MakeRequest(shifts, workers, availability, allowMovePreassigned: false);

        var result = await solver.SolveAsync(request, CancellationToken.None);

        Assert.False(result.Success);
        Assert.NotNull(result.FailureReason);
        Assert.Contains("Preassigned", result.FailureReason!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SolveAsync_LargerShifts_MultipleWorkersPerShift_CompletesAndAssignsCorrectCounts()
    {
        var solver = CreateSolver();

        var start = new DateOnly(2026, 02, 01);
        var days = 7;
        var workers = MakeWorkers(10);

        // Each day: Morning requires 2 workers, Evening requires 2 workers
        // Total slots = 7 * 4 = 28 (and no worker can do more than 1 per day due to placeholder)
        // With 10 workers, it should be solvable because each day needs 4 distinct workers.
        var shifts = new List<ShiftRequirement>();
        for (int d = 0; d < days; d++)
        {
            var date = start.AddDays(d);
            shifts.Add(MakeShift(date, ShiftType.Morning, requiredWorkers: 2));
            shifts.Add(MakeShift(date, ShiftType.Evening, requiredWorkers: 2));
        }

        var availability = new Dictionary<string, IReadOnlySet<string>>();
        var allShiftKeys = shifts.Select(s => ShiftKey.Of(s.Date, s.Shift)).Distinct();

        foreach (var w in workers)
            AddAvailability(availability, w.Id, allShiftKeys);

        var request = MakeRequest(shifts, workers, availability);

        var result = await solver.SolveAsync(request, CancellationToken.None);

        AssertAllAssignmentsAreValid(result);
        Assert.Equal(days * 2, result.Assignments.Count); // 2 shift-assignments per day

        // Each shift assignment should have exactly the required number of workers
        foreach (var a in result.Assignments)
        {
            var required = shifts.Single(s => s.Date == a.Date && s.Shift == a.Shift).RequiredWorkers;
            Assert.Equal(required, a.WorkerIds.Count);
        }

        AssertNoWorkerDoubleBookedSameDay(result);
    }

    [Fact]
    public async Task SolveAsync_TwoMonths_14Workers_ManyPreassignments_Completes()
    {
        var solver = CreateSolver();

        // "Two whole months" planning horizon (60 days is a simple approx)
        var start = new DateOnly(2026, 01, 01);
        var days = 60;

        var workers = MakeWorkers(14);

        // 2 shifts/day, each requires 1 worker => 120 shift-assignments
        var shifts = new List<ShiftRequirement>(days * 2);

        // Preassign every 3rd day Morning, every 5th day Evening, rotating workers.
        // This spreads preassignments across the horizon and forces the solver to route around them.
        for (int d = 0; d < days; d++)
        {
            var date = start.AddDays(d);

            var morningPre = Array.Empty<string>();
            if (d % 3 == 0)
                morningPre = new[] { workers[(d / 3) % workers.Length].Id };

            var eveningPre = Array.Empty<string>();
            if (d % 5 == 0)
                eveningPre = new[] { workers[(d / 5 + 7) % workers.Length].Id };

            shifts.Add(MakeShift(date, ShiftType.Morning, requiredWorkers: 1, preassigned: morningPre));
            shifts.Add(MakeShift(date, ShiftType.Evening, requiredWorkers: 1, preassigned: eveningPre));
        }

        // Availability:
        // - Everyone is available for almost everything,
        // - BUT introduce some "holes" to encourage backtracking:
        //   Each worker is unavailable for Evening on some periodic days, and Morning on other periodic days.
        var availability = new Dictionary<string, IReadOnlySet<string>>();

        foreach (var w in workers)
        {
            var keys = new HashSet<string>();

            for (int d = 0; d < days; d++)
            {
                var date = start.AddDays(d);

                // periodic holes based on worker index (stable, deterministic)
                var workerIndex = int.Parse(w.Id.AsSpan(1)); // "w7" -> 7
                var blockMorning = (d + workerIndex) % 11 == 0; // about ~1/11 mornings blocked per worker
                var blockEvening = (d + workerIndex) % 13 == 0; // about ~1/13 evenings blocked per worker

                if (!blockMorning)
                    keys.Add(ShiftKey.Of(date, ShiftType.Morning));

                if (!blockEvening)
                    keys.Add(ShiftKey.Of(date, ShiftType.Evening));
            }

            AddAvailability(availability, w.Id, keys);
        }

        // Ensure preassigned workers are actually available for their assigned shifts
        // (since we added holes, we must guarantee preassignments don't violate availability).
        foreach (var s in shifts)
        {
            if (s.PreassignedWorkerIds.Count == 0) continue;

            var wid = s.PreassignedWorkerIds[0];
            var key = ShiftKey.Of(s.Date, s.Shift);

            if (!availability.TryGetValue(wid, out var set) || !set.Contains(key))
            {
                // If a preassignment lands on a "hole", add that key back to keep the test solvable.
                var hs = availability[wid] as HashSet<string>;
                hs?.Add(key);
            }
        }

        var request = MakeRequest(shifts, workers, availability, allowMovePreassigned: false);

        var result = await solver.SolveAsync(request, CancellationToken.None);

        AssertAllAssignmentsAreValid(result);

        // 60 days * 2 shifts/day = 120 assignments (shift-level)
        Assert.Equal(days * 2, result.Assignments.Count);

        // Ensure the solver respected your placeholder "no double shift same day"
        AssertNoWorkerDoubleBookedSameDay(result);

        // Ensure preassignments were respected (since allowMovePreassigned=false)
        // We verify that if a shift had a preassigned worker, that worker appears in the output for that shift.
        var resultByShift = result.Assignments.ToDictionary(a => (a.Date, a.Shift));

        foreach (var s in shifts)
        {
            if (s.PreassignedWorkerIds.Count == 0) continue;

            var pre = s.PreassignedWorkerIds[0];
            var a = resultByShift[(s.Date, s.Shift)];

            Assert.Contains(pre, a.WorkerIds);
        }
    }

}
