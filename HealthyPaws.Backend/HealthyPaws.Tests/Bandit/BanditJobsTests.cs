using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit;
using HealthyPaws.Services.Bandit.Jobs;
using HealthyPaws.Services.Bandit.Publisher;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.Exceptions;
using HealthyPaws.Services.FeedingService;
using HealthyPaws.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace HealthyPaws.Tests.Bandit;

public class BanditJobsTests : IDisposable
{
    // 10.03.2026 06:00 UTC (07:00 Sarajevo); obrok u 10:00 lokalno = 09:00 UTC
    static readonly DateTimeOffset Now = new(2026, 3, 10, 6, 0, 0, TimeSpan.Zero);
    static readonly DateTimeOffset Baseline = new(2026, 3, 10, 9, 0, 0, TimeSpan.Zero);
    static readonly DateOnly Day = new(2026, 3, 10);

    readonly TestDb _db = new();
    readonly FakeClock _clock = new(Now);
    readonly FakeJobClient _jobs = new();
    readonly FakePublisher _pub = new();
    readonly FakeDevice _device = new();
    readonly FakeFeeding _feeding = new();

    public void Dispose() => _db.Dispose();

    HealthyPaws.Services.Bandit.Bandit NewBandit() => new(_db.Create(), _feeding, _clock);

    BanditJobs Sut() => new(NewBandit(), _pub, _device, _jobs, _clock,
        TimeZoneInfo.FindSystemTimeZoneById(BanditOptions.TimeZone), NullLogger<BanditJobs>.Instance);

    async Task<Guid> Seed(DecisionStatus status, int? arm = null, DateTimeOffset? baseline = null, int meal = 0)
    {
        var dog = (await _db.Create().Dogs.FirstOrDefaultAsync()) ?? await _db.AddDogAsync();
        var id = Guid.NewGuid();
        await using var ctx = _db.Create();
        ctx.BanditDecisions.Add(new BanditDecision
        {
            Id = id, DogId = dog.Id, MealIndex = meal, Day = Day, BaselineUtc = baseline ?? Baseline,
            Status = status, Arm = arm, CreatedAt = Now,
        });
        await ctx.SaveChangesAsync();
        return id;
    }

    async Task<BanditDecision> Load(Guid id) => await _db.Create().BanditDecisions.AsNoTracking().SingleAsync(x => x.Id == id);

    // ------------------------------------------------------------ PlanToday

    [Fact]
    public async Task PlanToday_creates_decisions_and_schedules_select_and_deadline()
    {
        var dog = await _db.AddDogAsync();
        _feeding.Slots = [new BanditSlot(dog.Id, 0, new DateTime(2026, 3, 10, 10, 0, 0))];

        await Sut().PlanToday();

        var d = await _db.Create().BanditDecisions.SingleAsync();
        Assert.Equal((DecisionStatus.Planned, Baseline, Day), (d.Status, d.BaselineUtc, d.Day));
        var select = _jobs.Single(nameof(BanditJobs.RequestSelect));
        var deadline = _jobs.Single(nameof(BanditJobs.SelectDeadline));
        Assert.Equal(Baseline.AddMinutes(-75), select.At);
        Assert.Equal(Baseline.AddMinutes(-62), deadline.At);
        Assert.Equal(d.Id, select.Args[0]);
    }

    [Fact]
    public async Task PlanToday_skips_slots_that_are_too_late_for_the_earliest_arm()
    {
        var dog = await _db.AddDogAsync();
        // baseline za 59 min: ranija opcija (-60) je već prošla -> preskoči
        _feeding.Slots = [new BanditSlot(dog.Id, 0, new DateTime(2026, 3, 10, 7, 59, 0))];
        await Sut().PlanToday();
        Assert.Empty(_jobs.Scheduled);
        Assert.Equal(0, await _db.Create().BanditDecisions.CountAsync());
    }

    [Fact]
    public async Task PlanToday_is_idempotent_and_does_not_double_schedule()
    {
        var dog = await _db.AddDogAsync();
        _feeding.Slots = [new BanditSlot(dog.Id, 0, new DateTime(2026, 3, 10, 10, 0, 0))];
        await Sut().PlanToday();
        await Sut().PlanToday();
        Assert.Equal(1, await _db.Create().BanditDecisions.CountAsync());
        Assert.Equal(2, _jobs.Scheduled.Count);
    }

    [Fact]
    public async Task PlanToday_uses_local_day_not_utc_day()
    {
        // 23:30 UTC 9. marta je već 00:30 10. marta u Sarajevu
        _clock.Now = new DateTimeOffset(2026, 3, 9, 23, 30, 0, TimeSpan.Zero);
        var dog = await _db.AddDogAsync();
        _feeding.Slots = [new BanditSlot(dog.Id, 0, new DateTime(2026, 3, 10, 10, 0, 0))];
        await Sut().PlanToday();
        Assert.Equal(Day, (await _db.Create().BanditDecisions.SingleAsync()).Day);
        Assert.Equal(Day, _feeding.RequestedDay);
    }

    // ------------------------------------------------------------ select

    [Fact]
    public async Task RequestSelect_moves_planned_to_requested_and_publishes()
    {
        var id = await Seed(DecisionStatus.Planned);
        await Sut().RequestSelect(id);
        Assert.Equal(DecisionStatus.Requested, (await Load(id)).Status);
        Assert.Equal([id], _pub.Selects);
    }

    [Fact]
    public async Task RequestSelect_retry_republishes_without_state_change()
    {
        var id = await Seed(DecisionStatus.Requested);
        await Sut().RequestSelect(id);
        Assert.Equal([id], _pub.Selects);
    }

    [Theory]
    [InlineData(DecisionStatus.Chosen)]
    [InlineData(DecisionStatus.Dispensed)]
    [InlineData(DecisionStatus.Skipped)]
    public async Task RequestSelect_ignores_decisions_past_requested(DecisionStatus status)
    {
        var id = await Seed(status, arm: 0);
        await Sut().RequestSelect(id);
        Assert.Empty(_pub.Selects);
    }

    [Fact]
    public async Task RequestSelect_unknown_decision_is_noop()
    {
        await Sut().RequestSelect(Guid.NewGuid());
        Assert.Empty(_pub.Selects);
    }

    [Fact]
    public async Task OnSelected_chooses_arm_and_schedules_dispense_at_baseline_plus_arm()
    {
        var id = await Seed(DecisionStatus.Requested);
        await Sut().OnSelected(id, -60);
        var d = await Load(id);
        Assert.Equal((DecisionStatus.Chosen, -60, false), (d.Status, d.Arm, d.ChosenByFallback));
        Assert.Equal(Baseline.AddMinutes(-60), _jobs.Single(nameof(BanditJobs.Dispense)).At);
    }

    [Fact]
    public async Task OnSelected_after_fallback_already_chose_is_ignored()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        await Sut().OnSelected(id, 60);
        Assert.Equal(0, (await Load(id)).Arm);
        Assert.Empty(_jobs.Scheduled);
    }

    [Fact]
    public async Task OnSelected_from_planned_is_not_allowed()
    {
        var id = await Seed(DecisionStatus.Planned);
        await Sut().OnSelected(id, 60);
        Assert.Equal(DecisionStatus.Planned, (await Load(id)).Status);
    }

    [Theory]
    [InlineData(DecisionStatus.Planned)]
    [InlineData(DecisionStatus.Requested)]
    public async Task SelectDeadline_falls_back_to_baseline_arm_zero(DecisionStatus from)
    {
        var id = await Seed(from);
        await Sut().SelectDeadline(id);
        var d = await Load(id);
        Assert.Equal((DecisionStatus.Chosen, 0, true), (d.Status, d.Arm, d.ChosenByFallback));
        Assert.Equal(Baseline, _jobs.Single(nameof(BanditJobs.Dispense)).At);
    }

    [Fact]
    public async Task SelectDeadline_does_not_override_a_real_choice()
    {
        var id = await Seed(DecisionStatus.Requested);
        await Sut().OnSelected(id, 60);
        await Sut().SelectDeadline(id);
        var d = await Load(id);
        Assert.Equal((60, false), (d.Arm, d.ChosenByFallback));
    }

    [Fact]
    public async Task Failure_to_schedule_dispense_reverts_to_requested_so_it_can_be_retried()
    {
        var id = await Seed(DecisionStatus.Requested);
        _jobs.ThrowOnCreate = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().OnSelected(id, 0));
        Assert.Equal(DecisionStatus.Requested, (await Load(id)).Status);
    }

    // ------------------------------------------------------------ dispense

    [Fact]
    public async Task Dispense_publishes_marks_dispensed_and_schedules_outcome_collection()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline;
        await Sut().Dispense(id, null);
        var d = await Load(id);
        Assert.Equal(DecisionStatus.Dispensed, d.Status);
        Assert.Equal([(id, d.DogId)], _pub.Dispenses);
        Assert.Equal(Baseline + BanditOptions.OutcomeDelay, _jobs.Single(nameof(BanditJobs.CollectOutcome)).At);
    }

    [Fact]
    public async Task Dispense_is_skipped_when_too_late()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline + BanditOptions.DispenseGrace + TimeSpan.FromSeconds(1);
        await Sut().Dispense(id, null);
        Assert.Equal(DecisionStatus.Skipped, (await Load(id)).Status);
        Assert.Empty(_pub.Dispenses);
    }

    [Fact]
    public async Task Dispense_exactly_at_grace_boundary_still_dispenses()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline + BanditOptions.DispenseGrace;
        await Sut().Dispense(id, null);
        Assert.Equal(DecisionStatus.Dispensed, (await Load(id)).Status);
    }

    [Fact]
    public async Task Dispense_respects_arm_offset()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: -60);
        _clock.Now = Baseline.AddMinutes(-60) + BanditOptions.DispenseGrace + TimeSpan.FromMinutes(1);
        await Sut().Dispense(id, null);
        Assert.Equal(DecisionStatus.Skipped, (await Load(id)).Status);  // "kasno" se računa od baseline + arm
    }

    [Fact]
    public async Task Dispense_publish_failure_keeps_chosen_for_hangfire_retry()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline;
        _pub.FailDispense = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Sut().Dispense(id, null));
        Assert.Equal(DecisionStatus.Chosen, (await Load(id)).Status);
        Assert.Empty(_jobs.Scheduled);
    }

    [Theory]
    [InlineData(DecisionStatus.Planned)]
    [InlineData(DecisionStatus.Dispensed)]
    [InlineData(DecisionStatus.Skipped)]
    public async Task Dispense_only_acts_on_chosen_decisions(DecisionStatus status)
    {
        var id = await Seed(status, arm: 0);
        _clock.Now = Baseline;
        await Sut().Dispense(id, null);
        Assert.Empty(_pub.Dispenses);
        Assert.Equal(status, (await Load(id)).Status);
    }

    [Fact]
    public async Task Dispense_twice_dispenses_once()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline;
        await Sut().Dispense(id, null);
        await Sut().Dispense(id, null);
        Assert.Single(_pub.Dispenses);
    }

    // ------------------------------------------------------------ reconcile

    [Fact]
    public async Task Reconcile_skips_stuck_chosen_beyond_grace()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline + BanditOptions.DispenseGrace + TimeSpan.FromMinutes(1);
        await Sut().Reconcile();
        Assert.Equal(DecisionStatus.Skipped, (await Load(id)).Status);
        Assert.Empty(_jobs.Enqueued);
    }

    [Fact]
    public async Task Reconcile_reenqueues_lost_dispense_job()
    {
        var id = await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline + BanditOptions.LostJobAfter + TimeSpan.FromSeconds(30);
        await Sut().Reconcile();
        Assert.Equal(DecisionStatus.Chosen, (await Load(id)).Status);
        Assert.Equal(id, Assert.Single(_jobs.Enqueued, j => j.Method == nameof(BanditJobs.Dispense)).Args[0]);
    }

    [Fact]
    public async Task Reconcile_leaves_not_yet_due_decisions_alone()
    {
        await Seed(DecisionStatus.Chosen, arm: 0);
        _clock.Now = Baseline.AddMinutes(-10);
        await Sut().Reconcile();
        Assert.Empty(_jobs.Enqueued);
    }

    // ------------------------------------------------------------ collect outcome

    [Fact]
    public async Task CollectOutcome_ready_saves_outcome_and_publishes_feedback()
    {
        var id = await Seed(DecisionStatus.Dispensed, arm: 0);
        _device.Result = new OutcomeResult(OutcomeKind.Ready, new OutcomeDto(100, 80, 120));
        await Sut().CollectOutcome(id, null);
        var d = await Load(id);
        Assert.Equal((DecisionStatus.FeedbackSent, 100d, 80d, 120d), (d.Status, d.DispensedG, d.EatenG, d.LatencyS));
        Assert.Equal([(id, new OutcomeDto(100, 80, 120))], _pub.Feedbacks);
    }

    [Fact]
    public async Task CollectOutcome_not_dispensed_ends_with_no_data()
    {
        var id = await Seed(DecisionStatus.Dispensed, arm: 0);
        _device.Result = new OutcomeResult(OutcomeKind.NotDispensed, null);
        await Sut().CollectOutcome(id, null);
        Assert.Equal(DecisionStatus.NoData, (await Load(id)).Status);
        Assert.Empty(_pub.Feedbacks);
    }

    [Fact]
    public async Task CollectOutcome_not_ready_throws_so_hangfire_retries()
    {
        var id = await Seed(DecisionStatus.Dispensed, arm: 0);
        _device.Result = new OutcomeResult(OutcomeKind.NotReady, null);
        await Assert.ThrowsAsync<OutcomeNotReadyException>(() => Sut().CollectOutcome(id, null));
        Assert.Equal(DecisionStatus.Dispensed, (await Load(id)).Status);
    }

    [Fact]
    public async Task CollectOutcome_feedback_publish_failure_reverts_for_retry()
    {
        var id = await Seed(DecisionStatus.Dispensed, arm: 0);
        _device.Result = new OutcomeResult(OutcomeKind.Ready, new OutcomeDto(100, 80, null));
        _pub.FailFeedback = true;
        await Sut().CollectOutcome(id, null);
        Assert.Equal(DecisionStatus.Dispensed, (await Load(id)).Status);
    }

    [Theory]
    [InlineData(DecisionStatus.Chosen)]
    [InlineData(DecisionStatus.FeedbackSent)]
    [InlineData(DecisionStatus.Completed)]
    public async Task CollectOutcome_only_acts_on_dispensed(DecisionStatus status)
    {
        var id = await Seed(status, arm: 0);
        _device.Result = new OutcomeResult(OutcomeKind.Ready, new OutcomeDto(100, 80, null));
        await Sut().CollectOutcome(id, null);
        Assert.Empty(_pub.Feedbacks);
        Assert.Equal(status, (await Load(id)).Status);
    }

    // ------------------------------------------------------------ cijeli životni ciklus

    [Fact]
    public async Task Full_lifecycle_planned_to_feedback_sent()
    {
        var dog = await _db.AddDogAsync();
        _feeding.Slots = [new BanditSlot(dog.Id, 0, new DateTime(2026, 3, 10, 10, 0, 0))];
        await Sut().PlanToday();
        var id = (await _db.Create().BanditDecisions.SingleAsync()).Id;

        _clock.Now = Baseline.AddMinutes(-75);
        await Sut().RequestSelect(id);
        await Sut().OnSelected(id, 60);
        _clock.Now = Baseline.AddMinutes(60);
        await Sut().Dispense(id, null);
        _clock.Now += BanditOptions.OutcomeDelay;
        _device.Result = new OutcomeResult(OutcomeKind.Ready, new OutcomeDto(120, 100, 200));
        await Sut().CollectOutcome(id, null);

        var d = await Load(id);
        Assert.Equal((DecisionStatus.FeedbackSent, 60, false), (d.Status, d.Arm, d.ChosenByFallback));
        Assert.Equal([id], _pub.Selects);
        Assert.Equal([(id, d.DogId)], _pub.Dispenses);
        Assert.Equal([(id, new OutcomeDto(120, 100, 200))], _pub.Feedbacks);
    }
}
