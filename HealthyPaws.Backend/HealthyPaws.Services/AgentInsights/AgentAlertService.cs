using HealhtyPaws.Models.Responses.Agent;
using HealhtyPaws.Models.SearchObjects;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.BaseServices;
using HealthyPaws.Services.Exceptions;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.AgentInsights;

public class AgentAlertService(HealthyPawsDbContext db, IMapper map, AuthorizationService auth)
    : BaseService<AgentAlertResponse, Alert, AgentAlertSearchObject>(db, map), IAgentAlertService
{
    public override Task<IQueryable<Alert>> ApplyFilter(IQueryable<Alert> queryable, AgentAlertSearchObject searchObject)
    {
        var userId = auth.GetUserId();
        queryable = queryable.Where(a => dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == a.DogId));
        if (searchObject.DogId != null)
            queryable = queryable.Where(a => a.DogId == searchObject.DogId);
        if (searchObject.IsOpen != null)
            queryable = searchObject.IsOpen.Value
                ? queryable.Where(a => a.ResolvedAt == null)
                : queryable.Where(a => a.ResolvedAt != null);
        return Task.FromResult(queryable);
    }

    protected override IQueryable<Alert> ApplyOrdering(IQueryable<Alert> queryable, AgentAlertSearchObject searchObject)
        => queryable.OrderBy(a => a.ResolvedAt != null).ThenByDescending(a => a.LastSeenAt).ThenByDescending(a => a.Id);

    public override Task<AgentAlertResponse> GetByIdAsync(int id) => throw new NotSupportedException();

    public async Task<AgentAlertResponse> ResolveAsync(long id)
    {
        var userId = auth.GetUserId();
        var alert = await dbContext.Alerts.FirstOrDefaultAsync(a => a.Id == id
                        && dbContext.UserDevices.Any(ud => ud.UserId == userId && ud.Device!.DogId == a.DogId))
                    ?? throw new NotFoundException("Nije pronađeno!");
        if (alert.ResolvedAt == null)
        {
            alert.ResolvedAt = DateTimeOffset.UtcNow;
            alert.ResolvedBy = AlertResolvedBy.User;
            await dbContext.SaveChangesAsync();
        }
        return mapper.Map<AgentAlertResponse>(alert);
    }
}
