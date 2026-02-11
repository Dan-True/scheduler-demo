using Microsoft.AspNetCore.Mvc.Testing;
using SchedulerDemo.DTO;
using SchedulerDemo.Solver.Helper;
using SchedulerDemo.Solver.Models;
using System.Net;
using System.Net.Http.Json;

public class ApiSchedulingTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiSchedulingTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PostSolveJob_ThenPollUntilReady_ReturnsOkWithResult()
    {
        var worker1 = "w1";
        var worker2 = "w2";

        // Arrange - build a minimal DTO
        var dto = new ScheduleJobRequestDTO
        {
            Workers = new()
            {
                new WorkerDTO { WorkerId = worker1, Name = "Alice", Skills = new() },
                new WorkerDTO { WorkerId = worker2, Name = "Bob",   Skills = new() }
            },
            Shifts = new()
            {
                new ShiftDTO
                {
                    Date = new DateOnly(2026, 01, 01),
                    ShiftType = ShiftTypeDTO.Morning,
                    RequiredWorkers = 1,
                    PreassignedWorkerIds = new List<string>()
                }
            },
            AvailabilityByWorkerId = new Dictionary<string, HashSet<string>>
            {
                ["w1"] = new HashSet<string> { ShiftKey.Of(new DateOnly(2026, 01, 01), ShiftType.Morning) },
                ["w2"] = new HashSet<string> { ShiftKey.Of(new DateOnly(2026, 01, 01), ShiftType.Morning) }
            },
            AllowMovePreassigned = false
        };

        // Act 1: start job
        var post = await _client.PostAsJsonAsync("/api/schedule-jobs/Solve", dto);

        if (post.StatusCode != HttpStatusCode.Accepted)
        {
            var body = await post.Content.ReadAsStringAsync();
            throw new Exception($"POST failed: {(int)post.StatusCode} {post.StatusCode}\n{body}");
        }

        Assert.Equal(HttpStatusCode.Accepted, post.StatusCode);

        var started = await post.Content.ReadFromJsonAsync<ScheduleJobResponseDTO>();
        Assert.NotNull(started);
        Assert.NotEqual(Guid.Empty, started!.JobId);

        // Act 2: poll job
        ScheduleJobResultDTO? result = null;

        for (var i = 0; i < 60; i++)
        {
            var get = await _client.GetAsync($"/api/schedule-jobs/{started.JobId}");

            if (get.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Accepted)
            {
                await Task.Delay(50);
                continue;
            }

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
            result = await get.Content.ReadFromJsonAsync<ScheduleJobResultDTO>();
            break;
        }

        // Assert - adapt to your result schema
        Assert.NotNull(result);
        // Example:
        Assert.True(result.Success);
    }


    [Fact]
    public async Task PostSolveJob_ComplexPlan_12WorkersOver14Days_Returns14Assignments()
    {
        var startDate = new DateOnly(2026, 01, 01);
        var days = 14;
        var workerCount = 12;

        var workers = Enumerable.Range(1, workerCount)
            .Select(i => new WorkerDTO { WorkerId = $"w{i}", Name = $"Worker {i}", Skills = new() })
            .ToList();

        var shifts = Enumerable.Range(0, days)
            .Select(d => new ShiftDTO
            {
                Date = startDate.AddDays(d),
                ShiftType = ShiftTypeDTO.Morning,
                RequiredWorkers = 1,
                PreassignedWorkerIds = new List<string>()
            })
            .ToList();

        // have some preassignments to test that logic, but not too many to make it unsolvable
        shifts[0].PreassignedWorkerIds.Add("w1");
        shifts[1].PreassignedWorkerIds.Add("w2");

        // Everyone is available for every day (Morning)
        var availability = new Dictionary<string, HashSet<string>>();
        foreach (var w in workers)
        {
            availability[w.WorkerId] = new HashSet<string>(
                Enumerable.Range(0, days)
                    .Select(d => ShiftKey.Of(startDate.AddDays(d), ShiftType.Morning))
            );
        }

        var dto = new ScheduleJobRequestDTO
        {
            Workers = workers,
            Shifts = shifts,
            AvailabilityByWorkerId = availability,
            AllowMovePreassigned = false
        };

        var jobId = await StartJobAsync(dto);
        var result = await PollResultAsync(jobId, TimeSpan.FromSeconds(10));

        Assert.True(result.Success);
        Assert.NotNull(result.Assignments);

        // Expect 14 shift-assignments back (one per day)
        Assert.Equal(days, result.Assignments.Count);

        // Each assignment should have exactly 1 worker (since RequiredWorkers = 1)
        Assert.All(result.Assignments, a => Assert.Single(a.WorkerIds));

        // Ensure all dates are covered once
        var datesReturned = result.Assignments.Select(a => a.Date).OrderBy(d => d).ToList();
        var expectedDates = Enumerable.Range(0, days).Select(d => startDate.AddDays(d)).OrderBy(d => d).ToList();
        Assert.Equal(expectedDates, datesReturned);

        // Optional: ensure only known workers are used
        var workerIds = workers.Select(w => w.WorkerId).ToHashSet();
        Assert.All(result.Assignments.SelectMany(a => a.WorkerIds), wid => Assert.Contains(wid, workerIds));
    }

    [Fact]
    public async Task SpamQueue_With25Jobs_AllCompleteSuccessfully()
    {
        const int jobCount = 25;

        ScheduleJobRequestDTO MakeDto(int seed)
        {
            var date = new DateOnly(2026, 01, 01).AddDays(seed % 7);

            return new ScheduleJobRequestDTO
            {
                Workers = new()
            {
                new WorkerDTO { WorkerId = "w1", Name = "Alice", Skills = new() },
                new WorkerDTO { WorkerId = "w2", Name = "Bob", Skills = new() }
            },
                Shifts = new()
            {
                new ShiftDTO
                {
                    Date = date,
                    ShiftType = ShiftTypeDTO.Morning,
                    RequiredWorkers = 1,
                    PreassignedWorkerIds = new List<string>()
                }
            },
                AvailabilityByWorkerId = new Dictionary<string, HashSet<string>>
                {
                    ["w1"] = new HashSet<string> { ShiftKey.Of(date, ShiftType.Morning) },
                    ["w2"] = new HashSet<string> { ShiftKey.Of(date, ShiftType.Morning) }
                },
                AllowMovePreassigned = false
            };
        }

        // Start many jobs quickly
        var jobIds = new List<Guid>(jobCount);
        for (int i = 0; i < jobCount; i++)
            jobIds.Add(await StartJobAsync(MakeDto(i)));

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        var pending = new HashSet<Guid>(jobIds);
        var completed = new Dictionary<Guid, ScheduleJobResultDTO>();

        var sawNotReady = false;

        while (pending.Count > 0 && DateTime.UtcNow < deadline)
        {
            foreach (var id in pending.ToArray())
            {
                var resp = await _client.GetAsync($"/api/schedule-jobs/{id}");

                if (resp.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Accepted)
                {
                    sawNotReady = true;
                    continue;
                }

                Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

                var result = await resp.Content.ReadFromJsonAsync<ScheduleJobResultDTO>();
                Assert.NotNull(result);

                completed[id] = result!;
                pending.Remove(id);
            }

            if (pending.Count > 0)
                await Task.Delay(50);
        }

        if (pending.Count > 0)
            throw new TimeoutException($"Timed out waiting for {pending.Count} jobs to finish.");

        Assert.Equal(jobCount, completed.Count);

        Assert.All(completed.Values, r =>
        {
            Assert.True(r.Success);
            Assert.NotNull(r.Assignments);
            Assert.NotEmpty(r.Assignments);
            Assert.All(r.Assignments, a => Assert.NotEmpty(a.WorkerIds));
        });
    }


    private async Task<Guid> StartJobAsync(ScheduleJobRequestDTO dto)
    {
        var post = await _client.PostAsJsonAsync("/api/schedule-jobs/Solve", dto);

        if (post.StatusCode != HttpStatusCode.Accepted)
        {
            var body = await post.Content.ReadAsStringAsync();
            throw new Exception($"POST failed: {(int)post.StatusCode} {post.StatusCode}\n{body}");
        }

        var started = await post.Content.ReadFromJsonAsync<ScheduleJobResponseDTO>();
        Assert.NotNull(started);
        Assert.NotEqual(Guid.Empty, started!.JobId);
        return started.JobId;
    }


    private async Task<ScheduleJobResultDTO> PollResultAsync(Guid jobId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            var get = await _client.GetAsync($"/api/schedule-jobs/{jobId}");

            if (get.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Accepted)
            {
                await Task.Delay(50);
                continue;
            }

            Assert.Equal(HttpStatusCode.OK, get.StatusCode);

            var result = await get.Content.ReadFromJsonAsync<ScheduleJobResultDTO>();
            Assert.NotNull(result);
            return result!;
        }

        throw new TimeoutException($"Job {jobId} did not complete within {timeout}.");
    }
}
