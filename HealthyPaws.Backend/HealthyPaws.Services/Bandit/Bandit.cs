using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.FeedingService;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Bandit;

public class Bandit(HealthyPawsDbContext dbContext, IFeedingService feedingService, TimeProvider clock)
    : IBandit
{
    public async Task<IReadOnlyList<(int DogId, int MealIndex, DateTimeOffset BaselineUtc)>> MealsForDay(DateOnly day)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(BanditOptions.TimeZone);
        var slots = await feedingService.GetBanditSlots(day);
        return slots.Select(s => (s.DogId, s.MealIndex, LocalToUtc(s.LocalBaseline, tz))).ToList();
    }

    static DateTimeOffset LocalToUtc(DateTime local, TimeZoneInfo tz)
    {
        return new DateTimeOffset(local, tz.GetUtcOffset(local)).ToUniversalTime();
    }

    public async Task<bool> TryCreate(Guid id, int dogId, int mealIndex, DateOnly day, DateTimeOffset baselineUtc)
    {
        await dbContext.BanditDecisions.AddAsync(new BanditDecision
        {
            Id = id, DogId = dogId, MealIndex = mealIndex, Day = day, BaselineUtc = baselineUtc,
            Status = DecisionStatus.Planned, CreatedAt = clock.GetUtcNow(),
        });
        try
        {
            await dbContext.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();
            var exists =
                await dbContext.BanditDecisions.AnyAsync(x =>
                    x.DogId == dogId && x.MealIndex == mealIndex && x.Day == day);
            if (exists) return false;
            throw;
        }
    }

    public Task<DecisionDto?> Get(Guid id) =>
        dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new DecisionDto(x.Id, x.DogId, x.MealIndex, x.BaselineUtc, x.Arm, x.Status))
            .SingleOrDefaultAsync();

    public async Task<IReadOnlyList<DecisionDto>> ListChosen() =>
        await dbContext.BanditDecisions.AsNoTracking().Where(x => x.Status == DecisionStatus.Chosen)
            .Select(x => new DecisionDto(x.Id, x.DogId, x.MealIndex, x.BaselineUtc, x.Arm, x.Status))
            .ToListAsync();

    public async Task<bool> Transition(Guid id, DecisionStatus from, DecisionStatus to)
    {
        DateTimeOffset? now = clock.GetUtcNow();
        var q = dbContext.BanditDecisions.Where(x => x.Id == id && x.Status == from);
        var rows = to switch
        {
            DecisionStatus.Requested => await q.ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, to).SetProperty(x => x.RequestedAt, now)),
            DecisionStatus.Dispensed => await q.ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, to).SetProperty(x => x.DispensedAt, now)),
            DecisionStatus.FeedbackSent => await q.ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, to).SetProperty(x => x.FeedbackSentAt, now)),
            DecisionStatus.Completed => await q.ExecuteUpdateAsync(s =>
                s.SetProperty(x => x.Status, to).SetProperty(x => x.CompletedAt, now)),
            _ => await q.ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, to)),
        };
        return rows == 1;
    }

    public async Task<bool> TryChoose(Guid id, int arm, DecisionStatus[] allowedFrom, bool byFallback = false)
    {
        DateTimeOffset? now = clock.GetUtcNow();
        int? armN = arm;
        var rows = await dbContext.BanditDecisions.Where(x => x.Id == id && Enumerable.Contains(allowedFrom, x.Status))
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Status, DecisionStatus.Chosen)
                .SetProperty(x => x.Arm, armN)
                .SetProperty(x => x.ChosenByFallback, byFallback)
                .SetProperty(x => x.ChosenAt, now));
        return rows == 1;
    }

    public async Task SaveOutcome(Guid id, OutcomeDto o)
    {
        double? dispensed = o.DispensedG, eaten = o.EatenG;
        await dbContext.BanditDecisions.Where(x => x.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.DispensedG, dispensed)
                .SetProperty(x => x.EatenG, eaten)
                .SetProperty(x => x.LatencyS, o.LatencyS));
    }
}
