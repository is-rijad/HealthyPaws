using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.AgentInsights;
using HealthyPaws.WebApi.Controllers.BaseControllers;
using Microsoft.AspNetCore.Mvc;

namespace HealthyPaws.WebApi.Controllers;

public class AgentLogsController(IAgentDecisionLogService service)
    : BaseGetController<AgentDecisionLogResponse, AgentDecisionLogSearchObject, IAgentDecisionLogService>(service)
{
    [HttpGet]
    public override async Task<IActionResult> Get(AgentDecisionLogSearchObject searchObject)
        => await base.Get(searchObject);
}
