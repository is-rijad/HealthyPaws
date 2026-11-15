using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

[ApiController]
public class OkController : ControllerBase
{
    [HttpGet("api/ok")]
    [AllowAnonymous]
    public async Task<IActionResult> OkAsync()
    {
        return Ok();
    }
}