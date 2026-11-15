using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity.Bandit;
using Microsoft.EntityFrameworkCore;

namespace HealthyPaws.Services.Bandit.Data;

public class BanditData(HealthyPawsDbContext dbContext, TimeProvider clock) : IBanditData
{
    public async Task<(int DogId, int MealIndex, BanditContext Context)> BuildContext(Guid decisionId)
    {
        var decision = await dbContext.BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == decisionId);

        var tz = TimeZoneInfo.FindSystemTimeZoneById(BanditOptions.TimeZone);

        var recent = await dbContext.BanditDecisions.AsNoTracking()
            .Where(x => x.DogId == decision.DogId && x.BaselineUtc < decision.BaselineUtc && x.EatenG != null && x.DispensedG > 0)
            .OrderByDescending(x => x.BaselineUtc)
            .Take(AvgMeals)
            .Select(x => new { x.EatenG, x.DispensedG })
            .ToListAsync();

        var lastDispensedAt = await dbContext.BanditDecisions.AsNoTracking()
            .Where(x => x.DogId == decision.DogId && x.BaselineUtc < decision.BaselineUtc && x.DispensedAt != null
                        && (x.DispensedG == null || x.DispensedG > 0))
            .OrderByDescending(x => x.DispensedAt)
            .Select(x => x.DispensedAt)
            .FirstOrDefaultAsync();

        double? hours = lastDispensedAt is { } t ? Math.Max((decision.BaselineUtc - t).TotalHours, 0.0) : null;
        double? avgEaten = recent.Count == 0 ? null : recent.Average(x => Fraction(x.EatenG!.Value, x.DispensedG!.Value));

        var windowStartUtc = decision.BaselineUtc.UtcDateTime.AddHours(-TreatWindowHours);
        var dailyKcal = await dbContext.DogPlans.AsNoTracking().Where(x => x.DogId == decision.DogId)
            .Select(x => (double?)x.DailyKcal).FirstOrDefaultAsync();
        double? treatShare = null;
        if (dailyKcal is > 0)
        {
            var treatKcal = await dbContext.TreatLogs.AsNoTracking()
                .Where(x => x.DogId == decision.DogId && x.GivenAt >= windowStartUtc && x.GivenAt < decision.BaselineUtc.UtcDateTime)
                .SumAsync(x => x.Calories);
            treatShare = Math.Clamp(treatKcal / dailyKcal.Value, 0.0, 1.0);
        }

        var local = TimeZoneInfo.ConvertTime(decision.BaselineUtc, tz);
        var baselineHour = local.TimeOfDay.TotalHours;

        return (decision.DogId, decision.MealIndex, new BanditContext(hours, avgEaten, treatShare, baselineHour, decision.MealIndex));
    }

    const int AvgMeals = 5;
    const double TreatWindowHours = 24;
    static double Fraction(double eaten, double dispensed) => Math.Clamp(eaten / dispensed, 0.0, 1.0);

    public async Task SaveContext(Guid decisionId, BanditContext c)
    {
        DateTimeOffset? now = clock.GetUtcNow();
        double? hours = c.HoursSinceLastMeal, avg = c.AvgEatenFraction, treat = c.TreatBudgetShare, hour = c.BaselineHour;
        await dbContext.BanditDecisions.Where(x => x.Id == decisionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.ContextSavedAt, now)
                .SetProperty(x => x.HoursSinceLastMeal, hours)
                .SetProperty(x => x.AvgEatenFraction, avg)
                .SetProperty(x => x.TreatBudgetShare, treat)
                .SetProperty(x => x.BaselineHour, hour));
    }

    public async Task<BanditContext> GetContext(Guid decisionId)
    {
        var d = await dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == decisionId)
            .Select(x => new { x.MealIndex, x.ContextSavedAt, x.HoursSinceLastMeal, x.AvgEatenFraction, x.TreatBudgetShare, x.BaselineHour })
            .SingleAsync();
        if (d.ContextSavedAt is not null)
            return new BanditContext(d.HoursSinceLastMeal, d.AvgEatenFraction, d.TreatBudgetShare, d.BaselineHour ?? 0, d.MealIndex);
        
        var (_, _, ctx) = await BuildContext(decisionId);
        await SaveContext(decisionId, ctx);
        return ctx;
    }

    public async Task<(int Version, string? Json)> GetState(int dogId)
    {
        var s = await dbContext.BanditStates.AsNoTracking().Where(x => x.DogId == dogId)
            .Select(x => new { x.Version, x.StateJson }).FirstOrDefaultAsync();
        return s is null ? (0, null) : (s.Version, s.StateJson);
    }

    public async Task<bool> TryApplyState(int dogId, int expectedVersion, string stateJson)
    {
        var now = clock.GetUtcNow();
        var rows = await dbContext.BanditStates.Where(x => x.DogId == dogId && x.Version == expectedVersion)
            .ExecuteUpdateAsync(s => s
                .SetProperty(x => x.Version, expectedVersion + 1)
                .SetProperty(x => x.StateJson, stateJson)
                .SetProperty(x => x.UpdatedAt, now));
        if (rows == 1) return true;
        if (expectedVersion != 0) return false;

        dbContext.BanditStates.Add(new BanditState { DogId = dogId, Version = 1, StateJson = stateJson, UpdatedAt = now });
        try { await dbContext.SaveChangesAsync(); return true; }
        catch (DbUpdateException) { dbContext.ChangeTracker.Clear(); return false; }
    }

    public Task<bool> TryApplyFeedback(Guid decisionId, int dogId, int expectedVersion, string stateJson, double reward)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(async () =>
        {
            dbContext.ChangeTracker.Clear();
            await using var tx = await dbContext.Database.BeginTransactionAsync();
            if (!await TryApplyState(dogId, expectedVersion, stateJson))
            {
                await tx.RollbackAsync();
                return false;
            }

            DateTimeOffset? now = clock.GetUtcNow();
            double? r = reward;
            var rows = await dbContext.BanditDecisions
                .Where(x => x.Id == decisionId && x.Status == DecisionStatus.FeedbackSent)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.Reward, r)
                    .SetProperty(x => x.Status, DecisionStatus.Completed)
                    .SetProperty(x => x.CompletedAt, now));
            if (rows != 1)
            {
                await tx.RollbackAsync();
                return false;
            }

            await tx.CommitAsync();
            return true;
        });
    }

    public async Task<(int DogId, int Arm)> GetDecisionArm(Guid decisionId)
    {
        var d = await dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == decisionId)
            .Select(x => new { x.DogId, x.Arm }).SingleAsync();
        return (d.DogId, d.Arm ?? throw new InvalidOperationException($"Odluka {decisionId} nema izabran arm."));
    }

    public Task<int> GetDogId(Guid decisionId) =>
        dbContext.BanditDecisions.AsNoTracking().Where(x => x.Id == decisionId).Select(x => x.DogId).SingleAsync();
}
