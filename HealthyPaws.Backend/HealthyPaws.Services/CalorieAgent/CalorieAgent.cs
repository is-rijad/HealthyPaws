using CalorieAgent;
using Grpc.Net.Client;

namespace HealthyPaws.Services.CalorieAgent;

public class CalorieAgentService : ICalorieAgentService
{
    private readonly string _rpcUrl = Environment.GetEnvironmentVariable("Rpc_Url") ?? throw new ArgumentException("Rpc_Url is not set!");

    public async Task<CalorieAgentResponse> TickAsync(CalorieAgentRequest request)
    {
        using var channel = GrpcChannel.ForAddress(_rpcUrl);
        var client = new global::CalorieAgent.CalorieAgent.CalorieAgentClient(channel);
        var response = await client.TickAsync(request);
        Console.WriteLine(response);
        return response;    
    }
}