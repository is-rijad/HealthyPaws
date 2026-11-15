using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using MapsterMapper;

namespace HealthyPaws.Services.AgentInsights;

public class AgentDecisionLogService(HealthyPawsDbContext db, IMapper map, AuthorizationService auth)
    : BaseService<AgentDecisionLogResponse, AgentDecisionLog, AgentDecisionLogSearchObject>(db, map), IAgentDecisionLogService
{
    public override Task<IQueryable<AgentDecisionLog>> ApplyFilter(IQueryable<AgentDecisionLog> queryable, AgentDecisionLogSearchObject searchObject)
    {
        var userId = auth.GetUserId();
        queryable = queryable.Where(l => dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == l.DogId));
        if (searchObject.DogId != null)
            queryable = queryable.Where(l => l.DogId == searchObject.DogId);
        return Task.FromResult(queryable);
    }

    protected override IQueryable<AgentDecisionLog> ApplyOrdering(IQueryable<AgentDecisionLog> queryable, AgentDecisionLogSearchObject searchObject)
        => queryable.OrderByDescending(l => l.ReceivedAt).ThenByDescending(l => l.Id);
}
