using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Services.BaseServices;

namespace HealthyPaws.Services.AgentInsights;

public interface IAgentAlertService : IBaseService<AgentAlertResponse, AgentAlertSearchObject>
{
    public Task<AgentAlertResponse> ResolveAsync(long id);
}
