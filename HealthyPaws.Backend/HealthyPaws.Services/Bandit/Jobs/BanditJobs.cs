using Hangfire;
using Hangfire.Server;
using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit.Publisher;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.Exceptions;
using Microsoft.Extensions.Logging;

namespace HealthyPaws.Services.Bandit.Jobs;

public class BanditJobs(
    IBandit bandit, IBanditPublisher pub, IDeviceCommand device,
    IBackgroundJobClient jobs, TimeProvider clock, TimeZoneInfo tz, ILogger<BanditJobs> log)
{
    const int MaxCollectRetries = 4;
    const int MaxDispenseRetries = 5;
    static readonly TimeSpan SelectLead = TimeSpan.FromMinutes(BanditOptions.SelectLeadMinutes);
    static readonly TimeSpan FallbackBeforeBaseline = TimeSpan.FromMinutes(BanditOptions.FallbackBeforeBaselineMinutes);
    static readonly TimeSpan EarliestArm = TimeSpan.FromMinutes(-BanditOptions.Arms.Min());
    static readonly TimeSpan OutcomeDelay = BanditOptions.OutcomeDelay;

    static DateTimeOffset DispenseAt(DecisionDto d) => d.BaselineUtc + TimeSpan.FromMinutes(d.Arm ?? 0);

    [AutomaticRetry(Attempts = 3)]
    public async Task PlanToday()
    {
        var now = clock.GetUtcNow();
        var day = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, tz).DateTime);

        foreach (var (dogId, mealIndex, baseline) in await bandit.MealsForDay(day))
        {
            var id = Guid.NewGuid();
            if (baseline - EarliestArm <= now || baseline - FallbackBeforeBaseline <= now) continue;
            if (!await bandit.TryCreate(id, dogId, mealIndex, day, baseline)) continue;

            jobs.Schedule<BanditJobs>(j => j.RequestSelect(id), baseline - SelectLead);
            jobs.Schedule<BanditJobs>(j => j.SelectDeadline(id), baseline - FallbackBeforeBaseline);
        }
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task RequestSelect(Guid id)
    {
        var decision = await bandit.Get(id);
        if (decision is null || decision.Status is not (DecisionStatus.Planned or DecisionStatus.Requested)) return;

        if (decision.Status == DecisionStatus.Planned &&
            !await bandit.Transition(id, DecisionStatus.Planned, DecisionStatus.Requested)) return;

        await pub.PublishSelect(id);
    }

    [AutomaticRetry(Attempts = 3)]
    public Task OnSelected(Guid id, int arm) =>
        ChooseAndSchedule(id, arm, [DecisionStatus.Requested], byFallback: false);

    [AutomaticRetry(Attempts = 3)]
    public Task SelectDeadline(Guid id) =>
        ChooseAndSchedule(id, 0, [DecisionStatus.Planned, DecisionStatus.Requested], byFallback: true);

    async Task ChooseAndSchedule(Guid id, int arm, DecisionStatus[] allowedFrom, bool byFallback)
    {
        if (!await bandit.TryChoose(id, arm, allowedFrom, byFallback)) return;
        try
        {
            await ScheduleDispense(id, arm);
        }
        catch
        {
            try { await bandit.Transition(id, DecisionStatus.Chosen, DecisionStatus.Requested); }
            catch (Exception revertEx) { log.LogError(revertEx, "Vraćanje odluke {Id} u Requested nije uspjelo; Reconcile će je pokupiti", id); }
            throw;
        }
    }

    async Task ScheduleDispense(Guid id, int armMinutes)
    {
        var decision = await bandit.Get(id);
        if (decision is null) return;
        jobs.Schedule<BanditJobs>(j => j.Dispense(id, null), decision.BaselineUtc + TimeSpan.FromMinutes(armMinutes));
    }

    [AutomaticRetry(Attempts = MaxDispenseRetries, DelaysInSeconds = [10, 30, 60, 120, 300])]
    public async Task Dispense(Guid id, PerformContext? context)
    {
        var decision = await bandit.Get(id);
        if (decision is not { Status: DecisionStatus.Chosen }) return;

        var due = DispenseAt(decision);
        if (clock.GetUtcNow() > due + BanditOptions.DispenseGrace)
        {
            log.LogWarning("Obrok {Id} preskočen: planirano {Due}, prekasno za ispuštanje", id, due);
            await bandit.Transition(id, DecisionStatus.Chosen, DecisionStatus.Skipped);
            return;
        }

        try
        {
            await pub.PublishDispense(id, decision.DogId);
        }
        catch (Exception ex) when (context?.GetJobParameter<int>("RetryCount") >= MaxDispenseRetries)
        {
            log.LogError(ex, "Ispuštanje za odluku {Id} nije uspjelo nakon svih pokušaja, preskačem", id);
            await bandit.Transition(id, DecisionStatus.Chosen, DecisionStatus.Skipped);
            throw;
        }

        if (await bandit.Transition(id, DecisionStatus.Chosen, DecisionStatus.Dispensed))
            jobs.Schedule<BanditJobs>(j => j.CollectOutcome(id, null), clock.GetUtcNow() + OutcomeDelay);
    }

    [AutomaticRetry(Attempts = 1)]
    public async Task Reconcile()
    {
        var now = clock.GetUtcNow();
        foreach (var d in await bandit.ListChosen())
        {
            var due = DispenseAt(d);
            if (now > due + BanditOptions.DispenseGrace)
            {
                if (await bandit.Transition(d.Id, DecisionStatus.Chosen, DecisionStatus.Skipped))
                    log.LogWarning("Odluka {Id} zaglavljena u Chosen, preskočena (planirano {Due})", d.Id, due);
            }
            else if (now > due + BanditOptions.LostJobAfter)
            {
                log.LogWarning("Dispense za odluku {Id} nije izvršen {Due}, zakazujem ponovo", d.Id, due);
                var id = d.Id;
                jobs.Enqueue<BanditJobs>(j => j.Dispense(id, null));
            }
        }
    }

    [AutomaticRetry(Attempts = MaxCollectRetries, DelaysInSeconds = [60, 120, 300, 600])]
    public async Task CollectOutcome(Guid id, PerformContext? context)
    {
        var decision = await bandit.Get(id);
        if (decision is not { Status: DecisionStatus.Dispensed }) return;

        var result = await device.ReadOutcome(id);
        switch (result.Kind)
        {
            case OutcomeKind.NotDispensed:
                await bandit.Transition(id, DecisionStatus.Dispensed, DecisionStatus.NoData);
                return;
            case OutcomeKind.NotReady:
                if (context?.GetJobParameter<int>("RetryCount") >= MaxCollectRetries)
                {
                    await bandit.Transition(id, DecisionStatus.Dispensed, DecisionStatus.NoData);
                    return;
                }
                throw new OutcomeNotReadyException();
        }

        var o = result.Outcome!;
        await bandit.SaveOutcome(id, o);

        if (!await bandit.Transition(id, DecisionStatus.Dispensed, DecisionStatus.FeedbackSent)) return;
        try
        {
            await pub.PublishFeedback(id, o);
        }
        catch
        {
            await bandit.Transition(id, DecisionStatus.FeedbackSent, DecisionStatus.Dispensed);
        }
    }
}
