using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Services.BaseServices;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers.BaseControllers;

public class BaseGetController<TResponse, TSearchObject, TService>(TService service) : BaseController where TResponse : class where TSearchObject : BaseSearchObject where TService : IBaseService<TResponse, TSearchObject> {
    public virtual async Task<PagedResult<TResponse>> Get([FromQuery] TSearchObject searchObject) {
        return await service.GetAsync(searchObject);
    }
    public virtual async Task<TResponse> GetById(int id) {
        return await service.GetByIdAsync(id);
    }
}