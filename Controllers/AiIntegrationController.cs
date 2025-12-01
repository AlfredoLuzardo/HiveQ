using HiveQ.Services;
using Microsoft.AspNetCore.Mvc;

namespace HiveQ.Controllers
{
    [ApiController]
    [Route("api/mcp")]
    public class AiIntegrationController : ControllerBase
    {
        private readonly IWaitTimePredictionService _predictionService;

        public AiIntegrationController(IWaitTimePredictionService predictionService)
        {
            _predictionService = predictionService;
        }

        // Tool: update_queue_estimates
        // Description: Triggers the AI logic to recalculate wait times for a specific queue
        [HttpPost("update-estimates/{queueId}")]
        public async Task<IActionResult> UpdateEstimates(int queueId)
        {
            await _predictionService.UpdateAllQueueWaitTimesAsync(queueId);
            return Ok(new { message = $"Wait times updated for queue {queueId}" });
        }
        
        // Tool: get_prediction
        // Description: Gets a specific prediction for a position without saving it
        [HttpGet("predict/{queueId}/{position}")]
        public async Task<IActionResult> GetPrediction(int queueId, int position)
        {
            var minutes = await _predictionService.CalculateEstimatedWaitTimeAsync(queueId, position);
            return Ok(new { estimatedMinutes = minutes });
        }
    }
}