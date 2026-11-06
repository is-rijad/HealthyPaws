using System.Globalization;
using CalorieAgent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using HealhtyPaws.Models.Responses.Calorie;
using HealhtyPaws.Models.Responses.Dog;
using HealthyPaws.Database;
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
using Microsoft.EntityFrameworkCore.Query;
using Action = CalorieAgent.Action;
using LearnedState = CalorieAgent.LearnedState;

namespace HealthyPaws.Services.CalorieAgent;

public class CalorieAgentService(HealthyPawsDbContext dbContext,
    IMapper mapper, 
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
        var response = await client.TickAsync(request);
        await HandleResponse(response);
        return response;    
    }

    private async Task<CalorieAgentRequest> PrepareRequest(Database.Entity.Dog dog, Database.Entity.DogWeight weight)
    {
        var dogWeights =  await dbContext.DogWeights.Where(d => d.DogId == dog.Id).OrderBy(d => d.CreatedAt).Skip(1).Take(8).ToListAsync();
        var breed = await dbContext.Breeds.SingleAsync(b => b.Id == dog.BreedId);
        var plan = await dbContext.DogPlans.Where(p => p.DogId == dog.Id).OrderByDescending(p => p.PlanVersion)
            .Include(dogPlan => dogPlan.Learned).FirstOrDefaultAsync();
        var learned = plan?.Learned;
        var feedingRecords = plan != null
            ? await dbContext.FeedingRecords.Where(fr => fr.PlanId == plan.DogId).GroupBy(fr => fr.EatenAt.Day).ToListAsync()
            : new();
        
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
                MeasuredAt = weight.CreatedAt.ToTimestamp()
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
                    CurrentDailyKcal = plan.DailyKcal,
                    PlanVersion = ++plan.PlanVersion,
                    Learned = learned == null
                        ? null
                        : new LearnedState()
                        {
                            ConsecutiveBadWeeks = learned.ConsecutiveBadWeeks,
                            LastUpdateAt = learned.LastUpdateAt?.ToTimestamp(),
                            NUpdates = learned.NUpdates,
                            WeeksAtBound = learned.WeeksAtBound,
                        }
                },
            Intake = plan == null
                ? null
                : new IntakeSummary()
                {
                    DaysCovered = feedingRecords.Count, // TODO: use real days covered
                    EatenGPerDay = feedingRecords.Count > 0 ? feedingRecords.Average(it => it.Sum(i => i.EatenG)) : 0,
                    OfferedGPerDay = plan.DailyGrams,
                    PeriodDays = feedingRecords.Count,
                }
        };
        if (request.Plan != null && learned != null)
        {
            if (learned.MerEstimateKcal.HasValue)
                request.Plan.Learned.MerEstimateKcal = learned.MerEstimateKcal.Value;
            if (learned.PendingWeightKg.HasValue)
                request.Plan.Learned.PendingWeightKg = learned.PendingWeightKg.Value;
            if (learned.VarianceKcal2.HasValue)
                request.Plan.Learned.VarianceKcal2 = learned.VarianceKcal2.Value;
        }
        if (dogWeights.Count > 0)
        {
            foreach (var dogWeight in dogWeights)
            {
                request.History.Add(new WeekRecord()
                {
                  Bcs  =  dogWeight.Bcs,
                  MeasuredAt = dogWeight.CreatedAt.ToUniversalTime().ToTimestamp(),
                  WeightKg =  dogWeight.Weight,
                  EatenGPerDay = dogWeight.EatenGPerDay!.Value,
                  OfferedGPerDay = dogWeight.OfferedGPerDay!.Value
                });
            }
        }
        return request;
    }

    private async Task<AgentOutcome> HandleResponse(CalorieAgentResponse d)
    {
        var measuredAt = DateTimeOffset.FromUnixTimeSeconds(d.MeasuredAt.Seconds);
        
        if (await dbContext.AgentDecisionLogs.AnyAsync(x => x.DogId == d.DogId && x.MeasuredAt == measuredAt))
        {
            return AgentOutcome.Duplicate;
        }

        var plan = await dbContext.DogPlans.Include(dogPlan => dogPlan.Learned)
            .SingleOrDefaultAsync(p => p.DogId == d.DogId);

        if (plan is null)
        {
            plan = new DogPlan() { DogId = d.DogId };
            await dbContext.DogPlans.AddAsync(plan);
        }

        AgentOutcome outcome;
        Alert? newAlert = null;

        if (plan.LastAppliedMeasuredAt is { } last && measuredAt <= last)
        {
            outcome = AgentOutcome.Stale;
        }
        else
        {
            plan.Learned = d.Learned.ToEntity();
            plan.LastAppliedMeasuredAt = measuredAt;
            plan.NextMeasureAt = d.NextEvaluationAt.ToDateTime();

            if (!d.PlanChanged)
            {
                outcome = AgentOutcome.NoPlanChange;
            }
            else if (d.PlanVersion == plan.PlanVersion && d.NewDailyKcal is { } kcal)
            {
                plan.DailyKcal = kcal;
                plan.DailyGrams = d.NewDailyGrams;
                plan.PlanVersion++;
                outcome = AgentOutcome.PlanApplied;
            }
            else
            {
                outcome = AgentOutcome.PlanVersionMismatch;
            }

            newAlert = await UpdateAlertsAsync(d, measuredAt);
        }

        await dbContext.AgentDecisionLogs.AddAsync(new AgentDecisionLog
        {
            DogId = d.DogId,
            MeasuredAt = measuredAt,
            CorrelationId = d.CorrelationId,
            Action = Enum<AgentAction>.Parse(d.Action.ToString()),
            Outcome = outcome,
            ReasonCodes = string.Join(",", d.ReasonCodes),
            Explanation = d.Explanation,
            NewDailyKcal = d.NewDailyKcal,
            NewDailyGrams = d.NewDailyGrams,
            PlanVersionInResponse = d.PlanVersion,
            ReceivedAt = DateTimeOffset.UtcNow,
            IsRejected = outcome != AgentOutcome.Stale && d.Action == Action.RequestRemeasure
        });

        await dbContext.SaveChangesAsync();
        
        if (newAlert is not null) await notificationService.SendAlertAsync(newAlert, d);

        return outcome;
    }
    
    private async Task<Alert?> UpdateAlertsAsync(CalorieAgentResponse d, DateTimeOffset at)
    {
        if (d.Action == Action.Escalate)
        {
            var reason = d.ReasonCodes.FirstOrDefault() ?? "UNKNOWN";
            var open = await dbContext.Alerts.FirstOrDefaultAsync(a => a.DogId == d.DogId && a.Reason == reason && a.ResolvedAt == null);
            if (open is not null)
            {
                open.LastSeenAt = at;
                open.Occurrences++;
                open.Explanation = d.Explanation;
                return null;
            }
            var created = new Alert { DogId = d.DogId, Reason = reason, Explanation = d.Explanation, FirstSeenAt = at, LastSeenAt = at };
            await dbContext.Alerts.AddAsync(created);
            return created;
        }

        if (d.Action != Action.RequestRemeasure)
        {
            var open = await dbContext.Alerts.Where(a => a.DogId == d.DogId && a.ResolvedAt == null).ToListAsync();
            foreach (var a in open) { a.ResolvedAt = at; a.ResolvedBy = AlertResolvedBy.Agent; }
        }
        return null;
    }
    
    public async Task<CalorieBudgetResponse> GetCalorieBudgetAsync(int dogId, DateTime? from, DateTime? to)
    {
        var userId = authorizationService.GetUserId();
        if (!await dbContext.UserDevices.AnyAsync(ud => ud.UserId == userId && ud.Device!.DogId == dogId))
            throw new NotFoundException("Nije pronađeno!");

        var dog = await dbContext.Dogs.AsNoTracking().FirstAsync(d => d.Id == dogId);
        var treatCalories = await treatLogService.GetTreatCaloriesAsync(dogId, from, to);
        var plan = await dbContext.DogPlans.AsNoTracking().SingleAsync(p => p.DogId == dogId);
        var eatenToday = await feedingService.CalculateEatenToday(dog.Id);
        
        return new CalorieBudgetResponse(dogId, Math.Round(plan.DailyKcal, 1), treatCalories, eatenToday);
    }
}