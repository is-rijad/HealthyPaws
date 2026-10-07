using System.Threading.Tasks;
using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealthyPaws.Services.Device;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class DevicesController(IDeviceService service) : BaseCrudController<DeviceInsertRequest, DeviceInsertRequest, DeviceResponse, BaseSearchObject, IDeviceService>(service)
{
    [HttpGet]
    public override async Task<PagedResult<DeviceResponse>> Get(BaseSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }
    [HttpGet("{id}")]
    public override async Task<DeviceResponse> GetById(int id)
    {
        return await base.GetById(id);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(DeviceInsertRequest request)
    {
        return await base.Insert(request);
    }
}