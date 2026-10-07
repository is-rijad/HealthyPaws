using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers.BaseControllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    
}