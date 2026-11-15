using HealhtyPaws.Models.Dto.Bandit;
using HealthyPaws.Services.Bandit.Jobs;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace HealthyPaws.Services.Bandit.Consumers;

public class SelectedConsumer(BanditJobs jobs, ILogger<SelectedConsumer> log)
    : IConsumer<SelectResponse>
{
    public async Task Consume(ConsumeContext<SelectResponse> c)
    {
        var m = c.Message;
        if (m.Arm is not { } arm || !BanditOptions.Arms.Contains(arm))
        {
            log.LogWarning("Arm {Arm} nije u dozvoljenom skupu (decision {Id})", m.Arm, m.DecisionId);
            return;
        }
        await jobs.OnSelected(Guid.Parse(m.DecisionId), arm);
    }
}