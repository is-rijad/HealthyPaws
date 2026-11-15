using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace HealthyPaws.Tests.Infrastructure;

/// <summary>
/// Pokreće pravi Python calorie-agent gRPC servis (healthy_paws.ai/.venv) na slobodnom portu, pa C# testovi
/// prolaze kroz cijeli lanac C# -> gRPC -> Python. Ako venv ne postoji, testovi se preskaču (Available = false).
/// </summary>
public sealed class PythonAgentFixture : IDisposable
{
    readonly Process? _process;
    public bool Available { get; }
    public string SkipReason { get; } = "";
    public string Url { get; } = "";

    public PythonAgentFixture()
    {
        var root = FindAiRoot();
        var python = root is null ? null : Path.Combine(root, ".venv", "bin", "python");
        if (root is null || python is null || !File.Exists(python))
        {
            SkipReason = "healthy_paws.ai/.venv/bin/python nije pronađen (pip install -r requirements.txt)";
            return;
        }

        var port = FreePort();
        var start = new ProcessStartInfo(python, [Path.Combine(root, "tests", "serve_grpc.py"), port.ToString()])
        {
            WorkingDirectory = root, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false,
        };
        start.Environment["PYTHONPATH"] = root;
        _process = Process.Start(start)!;

        var ready = Task.Run(() => _process.StandardOutput.ReadLine());
        if (!ready.Wait(TimeSpan.FromSeconds(30)) || ready.Result != "READY")
        {
            SkipReason = "Python gRPC servis se nije pokrenuo: " + (_process.HasExited ? _process.StandardError.ReadToEnd() : "timeout");
            Dispose();
            return;
        }

        Url = $"http://127.0.0.1:{port}";
        Available = true;
        // RpcClient čita Rpc_Url iz okoline pri konstrukciji
        Environment.SetEnvironmentVariable("Rpc_Url", Url);
    }

    static string? FindAiRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "healthy_paws.ai");
            if (Directory.Exists(candidate)) return candidate;
            dir = dir.Parent;
        }
        return null;
    }

    static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false }) { _process.Kill(true); _process.WaitForExit(5000); }
        }
        catch { /* proces je već završio */ }
        _process?.Dispose();
    }
}
