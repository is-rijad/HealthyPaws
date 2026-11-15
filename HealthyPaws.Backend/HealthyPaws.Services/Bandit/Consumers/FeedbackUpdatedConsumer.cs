using Hangfire;
using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Database.Entity.Bandit;
using HealthyPaws.Services.Bandit.Data;
using HealthyPaws.Services.Bandit.Jobs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HealthyPaws.Services.Bandit.Consumers;
public class FeedbackUpdatedConsumer(
    IBanditData data, IBandit bandit, IBackgroundJobClient jobs, ILogger<FeedbackUpdatedConsumer> log)
    : IConsumer<FeedbackResponse>
{
    public async Task Consume(ConsumeContext<FeedbackResponse> c)
    {
        var m = c.Message;
        var id = Guid.Parse(m.DecisionId);
        var dogId = int.Parse(m.DogId);
        var newVersion = m.State.GetProperty("version").GetInt32();

        if (await data.TryApplyFeedback(id, dogId, newVersion - 1, m.State.GetRawText(), m.Reward)) return;

        var decision = await bandit.Get(id);
        if (decision?.Status != DecisionStatus.FeedbackSent) return;

        log.LogWarning("Konflikt verzije za psa {Dog}, ponovo šaljem feedback {Id}", dogId, id);
        if (await bandit.Transition(id, DecisionStatus.FeedbackSent, DecisionStatus.Dispensed))
            jobs.Schedule<BanditJobs>(j => j.CollectOutcome(id, null), TimeSpan.FromSeconds(30));
    }
}