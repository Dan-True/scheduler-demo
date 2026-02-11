using SchedulerDemo.Scheduler;
using SchedulerDemo.Scheduler.JobStore;
using SchedulerDemo.Scheduler.Queue;
using SchedulerDemo.Solver;
using SchedulerDemo.Solver.Models;
using SchedulerDemo.Solver.Solver;
using SchedulerDemo.Solver.Solver.Solver;

namespace SchedulerDemo.JobWorker
{
    public class ScheduleJobWorker : BackgroundService
    {
        private readonly ISchedulingQueue _queue;
        private readonly IJobStore _store;
        private readonly ISearchSolver _solver;
        private readonly ILogger<ScheduleJobWorker> _logger;

        public ScheduleJobWorker(ISchedulingQueue queue, IJobStore store, ISearchSolver solver, ILogger<ScheduleJobWorker> logger)
        {
            _queue = queue; _store = store; _solver = solver; _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await foreach (var job in _queue.DequeueAllAsync(stoppingToken))
            {
                _store.MarkRunning(job.JobId);

                try
                {
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, job.CancellationToken);
                    var solveRequest = new SolveRequest
                    {
                        Workers = job.Request.Workers.ToList(),
                        Shifts = job.Request.Shifts.ToList(),
                        AvailabilityByWorkerId = job.Request.AvailabilityByWorkerId,
                        RulesetId = job.Request.RulesetId,
                        Options = job.Request.Options
                    };

                    var result = await _solver.SolveAsync(solveRequest, linkedCts.Token);

                    _store.MarkSucceeded(job.JobId, new ScheduleJobResult {
                        Success = result.Success,
                        Assignments = result.Assignments,
                        FailureReason = result.FailureReason
                    });
                }
                catch (OperationCanceledException)
                {
                    _store.MarkCancelled(job.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Job {JobId} failed", job.JobId);
                    _store.MarkFailed(job.JobId, ex.Message);
                }
            }
        }
    }
}
