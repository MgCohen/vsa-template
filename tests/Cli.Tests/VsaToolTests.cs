using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tests.Shared;

namespace Cli.Tests;

// Uses the packed tool as a consumer does: new app, add module, add request, doctor. Then builds and runs the app.
public sealed class VsaToolTests {
    [Fact]
    public async Task ScaffoldedAppBuildsAndRuns() {
        var ct = TestContext.Current.CancellationToken;
        var root = TempFolder.Create();
        // Each test run packs the same test version with new content. So the test uses its own NuGet cache, and its own
        // DOTNET_CLI_HOME: the tool resolver cache in it remembers the path of a local tool for each version.
        var env = new Dictionary<string, string> {
            ["NUGET_PACKAGES"] = Path.Combine(root, "nuget"),
            ["DOTNET_CLI_HOME"] = Path.Combine(root, "home"),
        };
        var app = Path.Combine(root, "Hello");
        try {
            // The global packages folder of the user is also a feed, so restore does not download each package again.
            var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
            Dotnet.Run(root, env, "tool", "exec", $"VSA.Cli@{LocalFeed.Version}", "--add-source", LocalFeed.Path, "-y", "--",
                "new", "app", "Hello", "--source", LocalFeed.Path, "--source", globalPackages).EnsureSuccess();
            Dotnet.Run(app, env, "vsa", "add", "module", "Billing").EnsureSuccess();
            Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();
            Assert.Contains($"OK: all use VSA {LocalFeed.Version}.", Dotnet.Run(app, env, "vsa", "doctor").EnsureSuccess().Output);

            var again = Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing");
            Assert.False(again.Succeeded);
            Assert.Contains("does not write over a file", again.Output);

            Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
            await RunAndCall(app, env, ct);

            var globalJson = Path.Combine(app, "global.json");
            File.WriteAllText(globalJson, File.ReadAllText(globalJson).Replace($"\"VSA.Sdk\": \"{LocalFeed.Version}\"", "\"VSA.Sdk\": \"0.0.1\""));
            var doctor = Dotnet.Run(app, env, "vsa", "doctor");
            Assert.False(doctor.Succeeded, doctor.Output);
            Assert.Contains("The versions are not the same", doctor.Output);
        } finally {
            TempFolder.Delete(root);
        }
    }

    // The status request of the module, the new request and the starter page.
    static async Task RunAndCall(string app, Dictionary<string, string> env, CancellationToken ct) {
        var url = $"http://127.0.0.1:{FreePort()}";
        using var process = Dotnet.Start(app, env, "run", "--project", "App/Host", "-c", "Release", "--no-build", "--no-launch-profile", "--urls", url);
        var output = new StringBuilder();
        process.OutputDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.ErrorDataReceived += (_, e) => { lock (output) { output.AppendLine(e.Data); } };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        try {
            using var client = new HttpClient { BaseAddress = new Uri(url) };
            await WaitUntilStarted(client, process, output, ct);

            Assert.Contains("\"status\":\"Ready\"", await client.GetStringAsync("/api/billing/status", ct));
            var created = await client.PostAsync("/api/billing/create-invoice", null, ct);
            Assert.True(created.IsSuccessStatusCode, $"POST /api/billing/create-invoice gave {created.StatusCode}.\n{output}");
            var page = await client.GetStringAsync("/billing", ct);
            Assert.Contains("<h1>Billing</h1>", page);
            Assert.Contains("Status: Ready", page);
        } finally {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }

    static async Task WaitUntilStarted(HttpClient client, Process app, StringBuilder output, CancellationToken ct) {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (DateTime.UtcNow < until) {
            Assert.False(app.HasExited, $"The app stopped with exit code {(app.HasExited ? app.ExitCode : 0)}.\n{output}");
            try {
                if ((await client.GetAsync("/", ct)).StatusCode == HttpStatusCode.OK) {
                    return;
                }
            } catch (HttpRequestException) {
            }
            await Task.Delay(500, ct);
        }
        Assert.Fail($"The app did not start in one minute.\n{output}");
    }

    static int FreePort() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
