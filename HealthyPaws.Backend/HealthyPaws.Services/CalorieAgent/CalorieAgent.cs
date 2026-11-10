using System.Globalization;
using CalorieAgent;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using HealhtyPaws.Models.Responses.Calorie;
using HealthyPaws.Database;
using HealthyPaws.Database.Entity;
using HealthyPaws.Database.Entity.CalorieAgent;
using HealthyPaws.Services.Authorization;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Services.Mapster;
using HealthyPaws.Services.Notifications;
using HealthyPaws.Services.TreatLog;
using Mapster.Utils;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Action = CalorieAgent.Action;
using LearnedState = CalorieAgent.LearnedState;

namespace HealthyPaws.Services.CalorieAgent;

public class CalorieAgentService(HealthyPawsDbContext dbContext,
    INotificationService notificationService, 
    ITreatLogService treatLogService,
    AuthorizationService authorizationService,
    IFeedingService feedingService) : ICalorieAgentService
{
    private readonly string _rpcUrl = Environment.GetEnvironmentVariable("Rpc_Url") ?? throw new ArgumentException("Rpc_Url is not set!");

    public async Task<CalorieAgentResponse> TickAsync(Database.Entity.Dog dog, Database.Entity.DogWeight weight)
    {
        using var channel = GrpcChannel.ForAddress(_rpcUrl);
        var client = new global::CalorieAgent.CalorieAgent.CalorieAgentClient(channel);
        var request = await PrepareRequest(dog, weight);
        CalorieAgentResponse response;
        try
        {
            response = await client.TickAsync(request);
        }
        catch (RpcException)
        {
            throw new BadRequestException("Kalorijski agent trenutno nije dostupan!");
        }
        await HandleResponse(response);
        return response;
    }

    private async Task<CalorieAgentRequest> PrepareRequest(Database.Entity.Dog dog, Database.Entity.DogWeight weight)
    {
        var rejectedMeasurementSeconds = (await dbContext.AgentDecisionLogs
                .Where(log => log.DogId == dog.Id && log.IsRejected)
                .Select(log => log.MeasuredAt)
                .ToListAsync())
            .Select(measuredAt => measuredAt.ToUnixTimeSeconds())
            .ToHashSet();
        var historyWeights = (await dbContext.DogWeights.Where(w => w.DogId == dog.Id).Where(w => w.Id != weight.Id)
                .OrderByDescending(w => w.CreatedAt).Take(8 + rejectedMeasurementSeconds.Count).ToListAsync())
            .Where(w => !rejectedMeasurementSeconds.Contains(w.CreatedAt.ToUniversalTime().ToTimestamp().Seconds))
            .Take(8)
            .ToList();
        historyWeights.Reverse();
        var breed = await dbContext.Breeds.SingleAsync(dogBreed => dogBreed.Id == dog.BreedId);
        var plan = await dbContext.DogPlans.Where(dogPlan => dogPlan.DogId == dog.Id).OrderByDescending(dogPlan => dogPlan.PlanVersion)
            .Include(dogPlan => dogPlan.Learned).FirstOrDefaultAsync();
        var learnedState = plan?.Learned;
        // Plan bez doze (DailyKcal <= 0, npr. kreiran uz prvo odbijeno mjerenje) tretira se kao "plan još ne postoji".
        var hasPlan = plan is { DailyKcal: > 0 };
        var feedingPeriod = await GetFeedingPeriod(dog.Id, hasPlan ? plan : null, weight);
        var (feedingByDay, periodDays) = (feedingPeriod.ByDay, feedingPeriod.PeriodDays);
        
        var request = new CalorieAgentRequest
        {
            CorrelationId = Guid.NewGuid().ToString(),
            History = {  },
            Measurement = new Measurement()
            {
                Bcs = weight.Bcs,
                DogId = dog.Id,
                Weight = weight.Weight,
                MaxWeight = (double)breed.MaxWeight,
                MinWeight = (double)breed.MinWeight,
                MeasuredAt = weight.CreatedAt.ToUniversalTime().ToTimestamp()
            },
            Profile = new DogProfile()
            {
                ActivityFactor = dog.ActivityStatus.ToString(CultureInfo.InvariantCulture),
                FoodKcalPerGram = weight.Calorie / 100,
                Neutered = dog.IsNeutered,
                PregnantOrLactating = false, // TODO: Check if use this field
                LifeStage = dog.LifeStage
            },
            Plan = plan == null
                ? null
                : new PlanState()
                {
                    PlanVersion = plan.PlanVersion,
                    Learned = learnedState == null
                        ? null
                        : new LearnedState()
                        {
                            ConsecutiveBadWeeks = learnedState.ConsecutiveBadWeeks,
                            LastUpdateAt = learnedState.LastUpdateAt?.ToTimestamp(),
                            NUpdates = learnedState.NUpdates,
                            WeeksAtBound = learnedState.WeeksAtBound,
                        }
                },
            Intake = !hasPlan
                ? null
                : new IntakeSummary()
                {
                    DaysCovered = Math.Min(feedingByDay.Count, periodDays),
                    OfferedGPerDay = plan!.DailyGrams,
                    PeriodDays = periodDays,
                }
        };
        if (hasPlan)
            request.Plan?.CurrentDailyKcal = plan!.DailyKcal;
        if (request.Intake != null && feedingByDay.Count > 0)
            request.Intake.EatenGPerDay = feedingByDay.Average(day => day.Sum(feedingRecord => feedingRecord.EatenG));
        if (request.Intake != null)
        {
            var treatKcalInPeriod = await treatLogService.GetTreatCaloriesAsync(dog.Id,
                DateTime.SpecifyKind(feedingPeriod.Start, DateTimeKind.Utc), DateTime.SpecifyKind(feedingPeriod.End, DateTimeKind.Utc));
            if (treatKcalInPeriod > 0)
                request.Intake.TreatKcalPerDay = treatKcalInPeriod / periodDays;
        }
        if (request.Plan != null && learnedState != null)
        {
            if (learnedState.MerEstimateKcal.HasValue)
                request.Plan.Learned.MerEstimateKcal = learnedState.MerEstimateKcal.Value;
            if (learnedState.PendingWeightKg.HasValue)
                request.Plan.Learned.PendingWeightKg = learnedState.PendingWeightKg.Value;
            if (learnedState.VarianceKcal2.HasValue)
                request.Plan.Learned.VarianceKcal2 = learnedState.VarianceKcal2.Value;
        }
        if (historyWeights.Count > 0)
        {
            foreach (var historyWeight in historyWeights)
            {
                var weekRecord = new WeekRecord()
                {
                    Bcs = historyWeight.Bcs,
                    MeasuredAt = historyWeight.CreatedAt.ToTimestamp(),
                    WeightKg = historyWeight.Weight,
                };
                if (historyWeight.EatenGPerDay.HasValue) weekRecord.EatenGPerDay = historyWeight.EatenGPerDay.Value;
                if (historyWeight.OfferedGPerDay.HasValue) weekRecord.OfferedGPerDay = historyWeight.OfferedGPerDay.Value;
                request.History.Add(weekRecord);
            }
        }
        return request;
    }

    private async Task<AgentOutcome> HandleResponse(CalorieAgentResponse response)
    {
        var measuredAt = DateTimeOffset.FromUnixTimeSeconds(response.MeasuredAt.Seconds);
        
        if (await dbContext.AgentDecisionLogs.AnyAsync(log => log.DogId == response.DogId && log.MeasuredAt == measuredAt))
        {
            return AgentOutcome.Duplicate;
        }

        var plan = await dbContext.DogPlans.Include(dogPlan => dogPlan.Learned)
            .SingleOrDefaultAsync(dogPlan => dogPlan.DogId == response.DogId);

        if (plan is null)
        {
            plan = new DogPlan() { DogId = response.DogId };
            await dbContext.DogPlans.AddAsync(plan);
        }

        AgentOutcome outcome;
        Alert? newAlert = null;

        if (plan.LastAppliedMeasuredAt is { } lastAppliedAt && measuredAt <= lastAppliedAt)
        {
            outcome = AgentOutcome.Stale;
        }
        else
        {
            if (plan.Learned is null) plan.Learned = response.Learned.ToEntity();
            else plan.Learned.CopyFrom(response.Learned);
            plan.LastAppliedMeasuredAt = measuredAt;
            plan.NextMeasureAt = response.NextEvaluationAt.ToDateTime();

            if (!response.PlanChanged)
            {
                outcome = AgentOutcome.NoPlanChange;
            }
            else if (response.PlanVersion == plan.PlanVersion && response.NewDailyKcal is { } newDailyKcal)
            {
                plan.DailyKcal = newDailyKcal;
                plan.DailyGrams = response.NewDailyGrams;
                plan.PlanVersion++;
                outcome = AgentOutcome.PlanApplied;
            }
            else
            {
                outcome = AgentOutcome.PlanVersionMismatch;
            }

            newAlert = await UpdateAlertsAsync(response, measuredAt);
        }

        await dbContext.AgentDecisionLogs.AddAsync(new AgentDecisionLog
        {
            DogId = response.DogId,
            MeasuredAt = measuredAt,
            CorrelationId = response.CorrelationId,
            Action = Enum<AgentAction>.Parse(response.Action.ToString()),
            Outcome = outcome,
            ReasonCodes = Truncate(string.Join(",", response.ReasonCodes), AgentDecisionLogEntityConfiguration.ReasonCodesMaxLength),
            Explanation = Truncate(response.Explanation, AgentDecisionLogEntityConfiguration.ExplanationMaxLength),
            RequiresHumanReview = response.RequiresHumanReview,
            Diagnostics = response.Diagnostics is null ? null : JsonFormatter.Default.Format(response.Diagnostics),
            NewDailyKcal = response.NewDailyKcal,
            NewDailyGrams = response.NewDailyGrams,
            PlanVersionInResponse = response.PlanVersion,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRejected = outcome != AgentOutcome.Stale && response.Action == Action.RequestRemeasure
        });

        try
        {
            await dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            if (await LogExistsAfterReset(response.DogId, measuredAt))
                return AgentOutcome.Duplicate;
            throw;
        }
        
        if (newAlert is not null) await notificationService.SendAlertAsync(newAlert, response);

        return outcome;
    }

    private async Task<bool> LogExistsAfterReset(int dogId, DateTimeOffset measuredAt)
    {
        dbContext.ChangeTracker.Clear();
        return await dbContext.AgentDecisionLogs.AnyAsync(log => log.DogId == dogId && log.MeasuredAt == measuredAt);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private sealed record FeedingPeriod(
        List<IGrouping<DateTime, FeedingRecord>> ByDay, int PeriodDays, DateTime Start, DateTime End);

    private async Task<FeedingPeriod> GetFeedingPeriod(int dogId, DogPlan? plan, Database.Entity.DogWeight weight)
    {
        const int defaultPeriodDays = 7;
        var periodEnd = weight.CreatedAt;
        if (plan is null)
            return new FeedingPeriod([], defaultPeriodDays, periodEnd.AddDays(-defaultPeriodDays), periodEnd);
        var lastEvaluationAt = await dbContext.AgentDecisionLogs
            .Where(log => log.DogId == dogId && log.Outcome != AgentOutcome.Stale)
            .OrderByDescending(log => log.MeasuredAt)
            .Select(log => (DateTimeOffset?)log.MeasuredAt)
            .FirstOrDefaultAsync();
        var periodStart = lastEvaluationAt is { } lastEvaluation && lastEvaluation.UtcDateTime < periodEnd
            ? lastEvaluation.UtcDateTime
            : periodEnd.AddDays(-defaultPeriodDays);
        var periodDays = Math.Max(1, (int)Math.Ceiling((periodEnd - periodStart).TotalDays));
        var byDay = (await dbContext.FeedingRecords
                .Where(feedingRecord => feedingRecord.PlanId == plan.Id && feedingRecord.EatenAt > periodStart && feedingRecord.EatenAt <= periodEnd)
                .ToListAsync())
            .GroupBy(feedingRecord => feedingRecord.EatenAt.Date)
            .ToList();
        return new FeedingPeriod(byDay, periodDays, periodStart, periodEnd);
    }
    
    private async Task<Alert?> UpdateAlertsAsync(CalorieAgentResponse response, DateTimeOffset occurredAt)
    {
        if (response.Action == Action.Escalate)
        {
            var reason = response.ReasonCodes.FirstOrDefault() ?? "UNKNOWN";
            var openAlert = await dbContext.Alerts.FirstOrDefaultAsync(alert => alert.DogId == response.DogId && alert.Reason == reason && alert.ResolvedAt == null);
            if (openAlert is not null)
            {
                openAlert.LastSeenAt = occurredAt;
                openAlert.Occurrences++;
                openAlert.Explanation = response.Explanation;
                return null;
            }
            var newAlert = new Alert { DogId = response.DogId, Reason = reason, Explanation = response.Explanation, FirstSeenAt = occurredAt, LastSeenAt = occurredAt };
            await dbContext.Alerts.AddAsync(newAlert);
            return newAlert;
        }

        if (response.Action != Action.RequestRemeasure)
        {
            var openAlerts = await dbContext.Alerts.Where(alert => alert.DogId == response.DogId && alert.ResolvedAt == null).ToListAsync();
            foreach (var alert in openAlerts) { alert.ResolvedAt = occurredAt; alert.ResolvedBy = AlertResolvedBy.Agent; }
        }
        return null;
    }
    
    public async Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to)
    {
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(userDevice => userDevice.UserId == userId && userDevice.Device!.DogId == dogId))
            throw new NotFoundException("Nije pronađeno!");

        var dog = await dbContext.Dogs.AsNoTracking().FirstAsync(dogEntity => dogEntity.Id == dogId);
        var treatCalories = await treatLogService.GetTreatCaloriesAsync(dogId, from, to);
        var plan = await dbContext.DogPlans.AsNoTracking().SingleAsync(dogPlan => dogPlan.DogId == dogId);
        var eatenToday = await feedingService.CalculateEatenToday(dog.Id);
        
        return new CalorieBudgetResponse(dogId, Math.Round(plan.DailyKcal, 1), treatCalories, eatenToday);
    }
}