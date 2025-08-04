using Microsoft.AspNetCore.Mvc;
using OllamaFarmer.Server.Dto.Sys;
using OllamaFarmer.Server.Services.Background.Interfaces;

namespace OllamaFarmer.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SysQueueController(ILogger<SysQueueController> logger,
        IBackgroundTaskQueue taskQueue
        )
        : ControllerBase
    {
        // need to wrap task queue queries in here fo rthe UI to manage the queue

        [HttpGet("count")]
        public ActionResult<int> GetQueueCount()
        {
            // This is a placeholder for actual queue count logic
            var queueCount = taskQueue.QueuedCount;
            return Ok(queueCount);
        }


        [HttpPost("dequeue")]
        public async Task<IActionResult> DequeueTask(CancellationToken cancellationToken)
        {
            try
            {
                var workItem = await taskQueue.DequeueAsync(cancellationToken);
                if (workItem == null)
                {
                    return NotFound("No tasks in the queue.");
                }
                // Execute the work item immediately for demonstration purposes
                await workItem(cancellationToken);
                return Ok("Task has been dequeued and executed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while dequeuing the task.");
                return StatusCode(500, "An error occurred while dequeuing the task.");
            }

        }

        [HttpPost("clear")]
        public async Task<IActionResult> ClearQueue()
        {
            try
            {
                await taskQueue.ClearAsync();
                return Ok("Queue has been cleared successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while clearing the queue.");
                return StatusCode(500, "An error occurred while clearing the queue.");
            }
        }

        [HttpGet("describe")]
        public async Task<ActionResult<TaskQueueDto>> DescribeQueue()
        {
            try
            {
                var queueDescription = await taskQueue.DescribeQueueAsync();
                return Ok(queueDescription);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while describing the queue.");
                return StatusCode(500, "An error occurred while describing the queue.");
            }
        }

        [HttpGet("items")]
        public async Task<ActionResult<List<TaskQueueItemDto>>> DescribeQueueItems()
        {
            try
            {
                var queueItems = await taskQueue.DescribeQueueItemsAsync();
                return Ok(queueItems);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while describing the queue items.");
                return StatusCode(500, "An error occurred while describing the queue items.");
            }
        }

        // current item
        [HttpGet("current")]
        public async Task<IActionResult> DescribeCurrentQueueItem()
        {
            try
            {
                var currentItem = await taskQueue.DescribeRunningQueueItemAsync();
                if (currentItem == null)
                {
                    return NotFound("No current item in the queue.");
                }
                return Ok(currentItem);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while getting the current queue item.");
                return StatusCode(500, "An error occurred while getting the current queue item.");
            }
        }



    }
}
