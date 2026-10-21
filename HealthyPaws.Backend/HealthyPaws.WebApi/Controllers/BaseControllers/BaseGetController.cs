using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Services.BaseServices;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers.BaseControllers;

public class BaseGetController<TResponse, TSearchObject, TService>(TService service) : BaseController where TResponse : class where TSearchObject : BaseSearchObject where TService : IBaseService<TResponse, TSearchObject> {
    public virtual async Task<IActionResult> Get([FromQuery] TSearchObject searchObject) {
        return Ok(await service.GetAsync(searchObject));
    }
    public virtual async Task<IActionResult> GetById(int id) {
        return  Ok(await service.GetByIdAsync(id));
    }
}