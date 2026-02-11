using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SchedulerDemo.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly HealthCheckService _healthChecks;

        public HealthController(HealthCheckService healthChecks)
            => _healthChecks = healthChecks;

        [HttpGet()]
        public IActionResult HealthCheck() { 
            return Ok(new { status = "Ok" });
        }


        [HttpGet("ready")]
        public async Task<IActionResult> Ready(CancellationToken ct)
        {
            var report = await _healthChecks.CheckHealthAsync(_ => true, ct);

            // Typical behavior: 200 if Healthy/Degraded, 503 if Unhealthy
            var statusCode = report.Status == HealthStatus.Unhealthy ? 503 : 200;

            return StatusCode(statusCode, new
            {
                status = report.Status.ToString(),
                checks = report.Entries.Select(e => new
                {
                    name = e.Key,
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description
                })
            });
        }
    }
}
