using Microsoft.AspNetCore.Mvc;
using SchedulerDemo.DTO;
using SchedulerDemo.Scheduler.Queue;
using SchedulerDemo.Scheduler.JobStore;

namespace SchedulerDemo.Controllers
{
    [ApiController]
    [Route("api/schedule-jobs")]
    public class SchedulingJobController : Controller
    {
        private readonly IJobStore _store;
        private readonly ISchedulingQueue _queue; 

        public SchedulingJobController(IJobStore store, ISchedulingQueue queue)
        {
            _store = store;
            _queue = queue;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns>Ok if the job is ready - result is in the response body. NoContent if not</returns>
        [HttpGet("{jobId}")]
        public ActionResult<ScheduleJobResultDTO> GetSolveJob([FromRoute] Guid jobId)
        {
            if (!_store.TryGet(jobId, out var state)) return NotFound();

            return state.Status switch
            {
                JobStatus.Succeeded => Ok(ScheduleJobResultDTO.MapFromScheduleJobResult(state.Result)),
                JobStatus.Failed => Ok(new ScheduleJobResultDTO { Error  = state.Error }),
                JobStatus.Cancelled => Ok(new ScheduleJobResultDTO { Cancelled = true }),
                _ => Accepted() // still working
            };
        }

        /// <summary>
        /// Deletes a job from being processed or removes the finished result
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        [HttpDelete("{jobId}")]
        public IActionResult DeleteSolveJob([FromRoute] Guid jobId)
        {
            // 400, 404 or 422 may be more appropriate to the restful purist, but we don't want it to be treated like an 4xx error by enterprise systems
            if (!_store.TryGet(jobId, out var state)) return NoContent(); // idempotent

            if (state.Status is JobStatus.Queued or JobStatus.Running)
                _store.TryCancel(jobId);

            _store.TryRemove(jobId);

            // Deletion successful gives 200
            return Ok();
        }

        /// <summary>
        /// Starts a solving job. 
        /// </summary>
        /// <returns>Returns a ScheduleJobResponse containing a jobId that can be used to query for the result or cancel the job</returns>
        [HttpPost("Solve")]
        public async Task<ActionResult<ScheduleJobResponseDTO>> PostSolveJob([FromBody] ScheduleJobRequestDTO scheduleJobRequest, CancellationToken ct)
        {
            // TODO insert validation of scheduleJobRequest here and return 400 if invalid

            // map from DTO to domain model
            var request = scheduleJobRequest.MapToScheduleJobRequest();
            var jobState = _store.Create(request);

            // enqueue the job for processing. The worker will update the job state in the store when it processes the job
            await _queue.EnqueueAsync(new ScheduleJob(jobState.JobId, request, ct), ct);

            // return to the caller
            var response = new ScheduleJobResponseDTO {
                JobId = jobState.JobId,
                Location = Url.Action(nameof(GetSolveJob), new { jobState.JobId }) ?? throw new InvalidOperationException("Could not generate location URL")
            };
            
            // job put on queue
            return Accepted(response);
        }
    }
}
