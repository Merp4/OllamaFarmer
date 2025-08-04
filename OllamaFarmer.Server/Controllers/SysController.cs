using Microsoft.AspNetCore.Mvc;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SysController(ILogger<SysController> logger)
        : ControllerBase
    {

        [HttpGet("status")]
        public IActionResult GetSystemStatus()
        {
            // This is a placeholder for actual system status logic
            var systemStatus = new
            {
                Status = "Running",
                Uptime = $"{Environment.TickCount / 1000} seconds"
            };
            return Ok(systemStatus);
        }
        [HttpGet("health")]
        public IActionResult GetHealthStatus()
        {
            // This is a placeholder for actual health check logic
            var healthStatus = new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow
            };
            return Ok(healthStatus);
        }

        [HttpGet("info")]
        public IActionResult GetSystemInfo()
        {
            // This is a placeholder for actual system information logic
            var systemInfo = new
            {
                OS = Environment.OSVersion.ToString(),
                MachineName = Environment.MachineName,
                ProcessorCount = Environment.ProcessorCount,
                MemoryAvailable = GC.GetTotalMemory(false)
            };
            return Ok(systemInfo);
        }

    }
}
