using System.Threading.Tasks;
using HealhtyPaws.Models.Requests.Auth;
using HealthyPaws.Services.Authorization;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class AuthController(AuthorizationService authorizationService) : BaseController
{
    [HttpGet]
    public async Task<IActionResult> Get() {
        return Ok(await authorizationService.GetNewToken());
    }

    [HttpGet("auth")]
    public IActionResult CheckAuth() {
        return Ok();
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] UserLoginRequest request) {
        return Ok(await authorizationService.Login(request));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request) {
        return Ok(await authorizationService.Refresh(request));
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout() {
        await authorizationService.Logout();
        return Ok();
    }
}