using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tests.Shared;

namespace Cli.Tests;

// Uses the packed tool as a consumer does: new app, add module, add request, doctor. Then builds and runs the app.
public sealed class VsaToolTests {
    string root = "";
    Dictionary<string, string> env = [];

    [SetUp]
    public void SetUp() {
        root = TempFolder.Create();
        // Each test run packs the same test version with new content. So the test uses its own NuGet cache, and its own
        // DOTNET_CLI_HOME: the tool resolver cache in it remembers the path of a local tool for each version.
        env = new Dictionary<string, string> {
            ["NUGET_PACKAGES"] = Path.Combine(root, "nuget"),
            ["DOTNET_CLI_HOME"] = Path.Combine(root, "home"),
        };
    }

    [TearDown]
    public void TearDown() => TempFolder.Delete(root);

    [Test]
    [CancelAfter(600_000)]
    public async Task ScaffoldedAppBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();
        Dotnet.Run(app, env, "vsa", "add", "module", "Billing").EnsureSuccess();
        Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();
        Assert.That(Dotnet.Run(app, env, "vsa", "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use VSA {LocalFeed.Version}."));

        var again = Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing");
        Assert.That(again.Succeeded, Is.False);
        Assert.That(again.Output, Does.Contain("does not write over a file"));

        Directory.CreateDirectory(Path.Combine(app, "Shared", "Pipeline"));
        var shared = Dotnet.Run(app, env, "vsa", "add", "module", "Pipeline");
        Assert.That(shared.Succeeded, Is.False);
        Assert.That(shared.Output, Does.Contain("The Shared module Pipeline exists"));
        Directory.Delete(Path.Combine(app, "Shared"), recursive: true);

        Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        Assert.That(Dotnet.Run(app, env, "test", "--solution", "Hello.slnx", "-c", "Release", "--no-build").EnsureSuccess().Output,
            Does.Contain("succeeded: 4"));
        await RunAndCall(app, ct, async (client, output) => {
            Assert.That(await client.GetStringAsync("/api/billing/status", ct), Does.Contain("\"status\":\"Ready\""));
            var created = await client.PostAsync("/api/billing/create-invoice", null, ct);
            Assert.That(created.IsSuccessStatusCode, Is.True, $"POST /api/billing/create-invoice gave {created.StatusCode}.\n{output}");
            var page = await client.GetStringAsync("/billing", ct);
            Assert.That(page, Does.Contain("<h1>Billing</h1>"));
            Assert.That(page, Does.Contain("Status: Ready"));
        });

        var globalJson = Path.Combine(app, "global.json");
        File.WriteAllText(globalJson, File.ReadAllText(globalJson).Replace($"\"VSA.Sdk\": \"{LocalFeed.Version}\"", "\"VSA.Sdk\": \"0.0.1\""));
        var doctor = Dotnet.Run(app, env, "vsa", "doctor");
        Assert.That(doctor.Succeeded, Is.False, doctor.Output);
        Assert.That(doctor.Output, Does.Contain("The versions are not the same"));
    }

    // A new app has an empty Modules/ folder. It must build and start before the first add module.
    [Test]
    [CancelAfter(600_000)]
    public async Task NewAppWithNoModuleBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();

        Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        await RunAndCall(app, ct, async (client, _) =>
            Assert.That(await client.GetStringAsync("/", ct), Does.Contain("<h1>Hello</h1>")));
    }

    // vsa new app Hello, with the packed tool from the local feed. Returns the folder of the app.
    string NewApp() {
        // The global packages folder of the user is also a feed, so restore does not download each package again.
        var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        Dotnet.Run(root, env, "tool", "exec", $"VSA.Cli@{LocalFeed.Version}", "--add-source", LocalFeed.Path, "-y", "--",
            "new", "app", "Hello", "--source", LocalFeed.Path, "--source", globalPackages).EnsureSuccess();
        return Path.Combine(root, "Hello");
    }

    // Starts the built app on a free port, waits until "/" answers, then calls the app. The output of the app is for
    // the failure messages.
    async Task RunAndCall(string app, CancellationToken ct, Func<HttpClient, StringBuilder, Task> calls) {
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
            await calls(client, output);
        } finally {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync(CancellationToken.None);
        }
    }

    static async Task WaitUntilStarted(HttpClient client, Process app, StringBuilder output, CancellationToken ct) {
        var until = DateTime.UtcNow.AddMinutes(1);
        while (DateTime.UtcNow < until) {
            Assert.That(app.HasExited, Is.False, () => $"The app stopped with exit code {app.ExitCode}.\n{output}");
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
