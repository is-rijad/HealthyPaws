using HealhtyPaws.Models.BaseModels;
using HealhtyPaws.Models.Requests.Device;
using HealhtyPaws.Models.Responses.Device;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.Device;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace HealthyPaws.WebApi.Controllers;

public class DevicesController(IDeviceService service) : BaseCrudController<DeviceInsertRequest, DeviceUpdateRequest, DeviceResponse, DeviceSearchObject, IDeviceService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(DeviceSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }
    [HttpGet("{id}")]
    public override async Task<IActionResult> GetById(int id)
    {
        return await base.GetById(id);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(DeviceInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    public override async Task<IActionResult> Update(int id, [FromBody] DeviceUpdateRequest request)
    {
        return await base.Update(id, request);
    }

}