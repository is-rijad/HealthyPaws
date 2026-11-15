using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.AgentInsights;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class AlertsController(IAgentAlertService service)
    : BaseGetController<AgentAlertResponse, AgentAlertSearchObject, IAgentAlertService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(AgentAlertSearchObject searchObject)
        => await base.Get(searchObject);

    [HttpPut("{id:long}/resolve")]
    public async Task<IActionResult> Resolve(long id) => Ok(await service.ResolveAsync(id));
}
