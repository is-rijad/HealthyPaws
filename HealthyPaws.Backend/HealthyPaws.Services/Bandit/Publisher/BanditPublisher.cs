using System.Text.Json;
using HealhtyPaws.Models.Dto.Bandit;
using HealhtyPaws.Models.Dto.Bandit.Outcome;
using HealthyPaws.Services.Bandit.Data;
using HealthyPaws.Services.Device;
using MassTransit;
using MassTransit.Serialization;

namespace HealthyPaws.Services.Bandit.Publisher;

public class BanditPublisher : IBanditPublisher
{
    private readonly ISendEndpointProvider _send;
    private readonly IBanditData _data;
    private readonly IDeviceCommand _device;
    private readonly string _selectQueue;
    private readonly string _feedbackQueue;

    public BanditPublisher(ISendEndpointProvider send, IBanditData data,
        IDeviceCommand device)
    {
        _send = send;
        _data = data;
        _device = device;
        _selectQueue = Environment.GetEnvironmentVariable("RMQ_LIN_UCB_SELECT_QUEUE") ?? throw new ArgumentException("RMQ_LIN_UCB_SELECT_QUEUE is not set!");
        _feedbackQueue = Environment.GetEnvironmentVariable("RMQ_LIN_UCB_FEEDBACK_QUEUE") ?? throw new ArgumentException("RMQ_LIN_UCB_FEEDBACK_QUEUE is not set!");
    }
    
    public async Task PublishSelect(Guid decisionId)
    {
        var dogId = await _data.GetDogId(decisionId);
        var ctx = await _data.GetContext(decisionId);
        var (_, json) = await _data.GetState(dogId);
        var req = new SelectRequest(decisionId.ToString(), dogId.ToString(), ctx, BanditOptions.Arms, ToElement(json));
        await Raw(_selectQueue, req);
    }

    public async Task PublishFeedback(Guid decisionId, OutcomeDto o)
    {
        var (dogId, arm) = await _data.GetDecisionArm(decisionId);
        var ctx = await _data.GetContext(decisionId);
        var (_, json) = await _data.GetState(dogId);
        var req = new FeedbackRequest(decisionId.ToString(), dogId.ToString(), arm, ctx,
            new OutcomePayload(o.DispensedG, o.EatenG, o.LatencyS), ToElement(json));
        await Raw(_feedbackQueue, req);
    }

    public async Task PublishDispense(Guid decisionId, int dogId) => await _device.Dispense(decisionId, dogId);

    static JsonElement? ToElement(string? json) => json is null ? null : JsonDocument.Parse(json).RootElement.Clone();

    async Task Raw<T>(string queue, T message) where T : class
    {
        var ep = await _send.GetSendEndpoint(new Uri($"queue:{queue}"));
        await ep.Send(message, ctx => ctx.Serializer = new SystemTextJsonRawMessageSerializer(RawSerializerOptions.AddTransportHeaders));
    }
}
