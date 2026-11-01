using CalorieAgent;
using HealthyPaws.Services.CalorieAgent;
using HealthyPaws.Services.MqPublisher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

[ApiController]
public class OkController(ICalorieAgentService calorieAgentService) : ControllerBase
{
    [HttpGet("api/ok")]
    [AllowAnonymous]
    public async Task<IActionResult> OkAsync()
    {
        await calorieAgentService.TickAsync(new CalorieAgentRequest()
        {
            DogId = 1,
            LastWeights =
            {
                new LastWeight()
                {
                    Bcs = 5,
                    MeasuredAt = DateTime.UtcNow.ToString(),
                    Weight = 5.9
                }
            }
        });
        return Ok();
    }
}