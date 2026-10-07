using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealthyPaws.Services.BaseServices;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers.BaseControllers;

public class BaseCrudController<TInsertRequest, TUpdateRequest, TResponse, TSearchObject, TService>(TService service) : BaseGetController<TResponse, TSearchObject, TService>(service) where TResponse : class where TSearchObject : BaseSearchObject where TInsertRequest : class where TUpdateRequest : class where TService : IBaseCrudService<TInsertRequest, TUpdateRequest, TResponse, TSearchObject> {
    public virtual async Task<IActionResult> Insert([FromBody] TInsertRequest request) {
        return Ok(await service.InsertAsync(request));
    }
    public virtual async Task<IActionResult> Update(int id, [FromBody] TUpdateRequest request) {
        return Ok(await service.UpdateAsync(id, request));
    }
    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(int id) {
        await service.DeleteAsync(id);
        return Ok();
    }
}