using Grpc.Net.Client;

namespace HealthyPaws.Services.RpcClient;

public class RpcClient : IDisposable, IAsyncDisposable
{
    private readonly string _rpcUrl = Environment.GetEnvironmentVariable("Rpc_Url") ?? throw new ArgumentException("Rpc_Url is not set!");
    public readonly GrpcChannel Channel;

    public RpcClient()
    {
        Channel = GrpcChannel.ForAddress(_rpcUrl);
    }
    
    public void Dispose()
    {
        Channel.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        Channel.Dispose();
        return ValueTask.CompletedTask;
    }
}