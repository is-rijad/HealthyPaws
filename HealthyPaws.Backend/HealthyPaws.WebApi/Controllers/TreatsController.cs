using HealhtyPaws.Models.Requests.Treat;
using HealhtyPaws.Models.Responses.Treat;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.Treat;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class TreatsController(ITreatService service) : BaseCrudController<TreatInsertRequest, TreatInsertRequest, TreatResponse, TreatSearchObject, ITreatService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(TreatSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(TreatInsertRequest request)
    {
        return await base.Insert(request);
    }

    [HttpPut("{id}")]
    public override async Task<IActionResult> Update(int id, [FromBody] TreatInsertRequest request)
    {
        return await base.Update(id, request);
    }
}
