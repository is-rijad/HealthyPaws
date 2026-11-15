using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Services.Bandit.Publisher;
using HealthyPaws.Services.Device;
using HealthyPaws.Services.FeedingService;

namespace HealthyPaws.Tests.Infrastructure;


public sealed class FakeFeeding : IFeedingService
{
    public IReadOnlyList<BanditSlot> Slots { get; set; } = [];
    public DateOnly? RequestedDay { get; private set; }
    public Task<double> CalculateEatenToday(int dogId) => Task.FromResult(0d);
    public Task<IReadOnlyList<BanditSlot>> GetBanditSlots(DateOnly day) { RequestedDay = day; return Task.FromResult(Slots); }
    public Task<double> GetPortionGrams(int dogId, int mealIndex) => Task.FromResult(0d);
}

public sealed class FakePublisher : IBanditPublisher
{
    public List<Guid> Selects { get; } = [];
    public List<(Guid, int)> Dispenses { get; } = [];
    public List<(Guid, OutcomeDto)> Feedbacks { get; } = [];
    public bool FailDispense { get; set; }
    public bool FailFeedback { get; set; }
    public Task PublishSelect(Guid decisionId) { Selects.Add(decisionId); return Task.CompletedTask; }
    public Task PublishFeedback(Guid decisionId, OutcomeDto o)
    {
        if (FailFeedback) throw new InvalidOperationException("broker nedostupan");
        Feedbacks.Add((decisionId, o)); return Task.CompletedTask;
    }
    public Task PublishDispense(Guid decisionId, int dogId)
    {
        if (FailDispense) throw new InvalidOperationException("uređaj nedostupan");
        Dispenses.Add((decisionId, dogId)); return Task.CompletedTask;
    }
}

public sealed class FakeDevice : IDeviceCommand
{
    public OutcomeResult Result { get; set; } = new(OutcomeKind.NotReady, null);
    public Task Dispense(Guid decisionId, int dogId) => Task.CompletedTask;
    public Task<OutcomeResult> ReadOutcome(Guid decisionId) => Task.FromResult(Result);
}

public sealed record ScheduledJob(string Method, object?[] Args, DateTimeOffset? At);

public sealed class FakeJobClient : IBackgroundJobClient
{
    public List<ScheduledJob> Scheduled { get; } = [];
    public List<ScheduledJob> Enqueued { get; } = [];
    public bool ThrowOnCreate { get; set; }

    public ScheduledJob Single(string method) => Assert.Single(Scheduled, j => j.Method == method);

    public string Create(Job job, IState state)
    {
        if (ThrowOnCreate) throw new InvalidOperationException("hangfire nedostupan");
        switch (state)
        {
            case ScheduledState s:
                Scheduled.Add(new ScheduledJob(job.Method.Name, job.Args.ToArray(), new DateTimeOffset(s.EnqueueAt, TimeSpan.Zero)));
                break;
            case EnqueuedState:
                Enqueued.Add(new ScheduledJob(job.Method.Name, job.Args.ToArray(), null));
                break;
        }
        return Guid.NewGuid().ToString();
    }

    public bool ChangeState(string jobId, IState state, string expectedState) => true;
}
