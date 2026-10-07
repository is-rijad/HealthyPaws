using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.User;
using HealhtyPaws.Models.Responses.User;
using HealthyPaws.Services.User;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class UsersController(IUserService service) : BaseCrudController<UserInsertRequest, UserInsertRequest, UserResponse, BaseSearchObject, IUserService>(service)
{
    [AllowAnonymous]
    [HttpPost]
    public override async Task<IActionResult> Insert(UserInsertRequest request)
    {
        return await base.Insert(request);
    }
}