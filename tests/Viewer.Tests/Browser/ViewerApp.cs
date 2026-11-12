using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace Viewer.Tests.Browser;

public sealed class ViewerApp : IDisposable
{
    private static readonly TimeSpan startLimit = TimeSpan.FromSeconds(60);

    private readonly Process process;

    private ViewerApp(Process process, string url)
    {
        this.process = process;
        Url = url;
    }

    public string Url { get; }

    public static async Task<ViewerApp> Start()
    {
        string host = HostPath();
        string url = $"http://127.0.0.1:{FreePort()}";
        ProcessStartInfo start = new("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(host)!,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        start.ArgumentList.Add(host);
        start.ArgumentList.Add("--urls");
        start.ArgumentList.Add(url);
        start.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";
        Process process = Process.Start(start)!;
        process.OutputDataReceived += (_, _) => { };
        process.ErrorDataReceived += (_, _) => { };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        ViewerApp app = new(process, url);
        await app.WaitUntilReady();
        return app;
    }

    public void Dispose()
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
        }
        process.Dispose();
    }

    private static string HostPath()
    {
        return typeof(ViewerApp).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(a => a.Key == "ViewerHost").Value!;
    }

    private static int FreePort()
    {
        using TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private async Task WaitUntilReady()
    {
        using HttpClient client = new();
        Stopwatch clock = Stopwatch.StartNew();
        while (clock.Elapsed < startLimit)
        {
            if (process.HasExited)
            {
                throw new InvalidOperationException($"The viewer stopped with exit code {process.ExitCode}.");
            }
            try
            {
                HttpResponseMessage response = await client.GetAsync(Url);
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch (HttpRequestException)
            {
            }
            await Task.Delay(250);
        }
        throw new TimeoutException($"The viewer did not answer at {Url} in {startLimit.TotalSeconds} seconds.");
    }
}
