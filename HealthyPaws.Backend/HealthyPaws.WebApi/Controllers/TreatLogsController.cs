using HealhtyPaws.Models.Requests.TreatLog;
using HealhtyPaws.Models.Responses.TreatLog;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.TreatLog;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class TreatLogsController(ITreatLogService service) : BaseCrudController<TreatLogInsertRequest, TreatLogInsertRequest, TreatLogResponse, TreatLogSearchObject, ITreatLogService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(TreatLogSearchObject searchObject)
    {
        return await base.Get(searchObject);
    }

    [HttpPost]
    public override async Task<IActionResult> Insert(TreatLogInsertRequest request)
    {
        return await base.Insert(request);
    }
}
