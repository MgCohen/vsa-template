using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tests.Shared;

namespace Cli.Tests;

// Uses the packed tool as a consumer does: new app, add module, add request, add command, add plugin, doctor, update. Then builds and runs the app.
public sealed class VsaToolTests {
    string root = "";
    Dictionary<string, string> env = [];

    [SetUp]
    public void SetUp() {
        root = TempFolder.Stable(TestContext.CurrentContext.Test.FullName);
        // Each test run packs the same test version with new content. So the test uses its own NuGet cache, and its own
        // DOTNET_CLI_HOME: the tool resolver cache in it remembers the path of a local tool for each version.
        env = new Dictionary<string, string> {
            ["NUGET_PACKAGES"] = Path.Combine(root, "nuget"),
            ["DOTNET_CLI_HOME"] = Path.Combine(root, "home"),
        };
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task ScaffoldedAppBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();
        Dotnet.Run(app, env, "vsa", "add", "module", "Billing").EnsureSuccess();
        Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();
        // The first command writes the command projects and adds them to the solution. The second command uses them.
        Dotnet.Run(app, env, "vsa", "add", "command", "ChangeLimit", "-m", "Billing").EnsureSuccess();
        Dotnet.Run(app, env, "vsa", "add", "command", "CloseInvoice", "-m", "Billing").EnsureSuccess();
        Assert.That(File.ReadAllText(Path.Combine(app, "Hello.slnx")), Does.Contain("Billing.Handlers.Commands.csproj"));
        Assert.That(Dotnet.Run(app, env, "vsa", "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use VSA {LocalFeed.Version}."));

        var again = Dotnet.Run(app, env, "vsa", "add", "request", "CreateInvoice", "-m", "Billing");
        Assert.That(again.Succeeded, Is.False);
        Assert.That(again.Output, Does.Contain("does not write over a file"));

        var command = Dotnet.Run(app, env, "vsa", "add", "command", "ChangeLimit", "-m", "Billing");
        Assert.That(command.Succeeded, Is.False);
        Assert.That(command.Output, Does.Contain("does not write over a file"));

        var unknown = Dotnet.Run(app, env, "vsa", "add", "command", "ChangeLimit", "-m", "Shipping");
        Assert.That(unknown.Succeeded, Is.False);
        Assert.That(unknown.Output, Does.Contain("The module Shipping does not exist"));

        Directory.CreateDirectory(Path.Combine(app, "Plugins", "Pipeline"));
        var plugin = Dotnet.Run(app, env, "vsa", "add", "module", "Pipeline");
        Assert.That(plugin.Succeeded, Is.False);
        Assert.That(plugin.Output, Does.Contain("The plugin module Pipeline exists"));
        Directory.Delete(Path.Combine(app, "Plugins", "Pipeline"), recursive: true);

        Dotnet.Run(app, env, "vsa", "add", "plugin", "Game").EnsureSuccess();
        Assert.That(File.Exists(Path.Combine(app, "Plugins", "Game", "GamePlugin.cs")), Is.True);
        Assert.That(File.Exists(Path.Combine(app, "Plugins", "Game", "Contracts", "Commands", "GameStep.cs")), Is.True);
        var pluginAgain = Dotnet.Run(app, env, "vsa", "add", "plugin", "Game");
        Assert.That(pluginAgain.Succeeded, Is.False);
        Assert.That(pluginAgain.Output, Does.Contain("The plugin module Game exists"));
        var pluginOfModule = Dotnet.Run(app, env, "vsa", "add", "plugin", "Billing");
        Assert.That(pluginOfModule.Succeeded, Is.False);
        Assert.That(pluginOfModule.Output, Does.Contain("The module Billing exists"));

        Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        Assert.That(Dotnet.Run(app, env, "test", "--solution", "Hello.slnx", "-c", "Release", "--no-build").EnsureSuccess().Output,
            Does.Contain("failed: 0").And.Match(@"succeeded: [1-9]\d*"));
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

        var update = Dotnet.Run(app, env, "vsa", "update", "--version", LocalFeed.Version).EnsureSuccess();
        Assert.That(update.Output, Does.Contain("global.json"));
        Assert.That(Dotnet.Run(app, env, "vsa", "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use VSA {LocalFeed.Version}."));
    }

    // A new app has an empty Features/ folder. It must build and start before the first add module.
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
        var host = Path.Combine(app, "App", "Host");
        var dll = Directory.GetFiles(Path.Combine(host, "bin", "Release"), "App.Host.dll", SearchOption.AllDirectories).Single();
        using var process = Dotnet.Start(host, env, dll, "--urls", url);
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

    static readonly TimeSpan StartLimitForVirusScannedBuild = TimeSpan.FromMinutes(3);

    static async Task WaitUntilStarted(HttpClient client, Process app, StringBuilder output, CancellationToken ct) {
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < StartLimitForVirusScannedBuild) {
            Assert.That(app.HasExited, Is.False, () => $"The app stopped with exit code {app.ExitCode}.\n{output}");
            try {
                if ((await client.GetAsync("/", ct)).StatusCode == HttpStatusCode.OK) {
                    TestContext.Out.WriteLine($"The app started in {(DateTime.UtcNow - started).TotalSeconds:0} s.");
                    return;
                }
            } catch (HttpRequestException) {
            }
            await Task.Delay(500, ct);
        }
        Assert.Fail($"The app did not start in {StartLimitForVirusScannedBuild.TotalMinutes} minutes.\n{output}");
    }

    static int FreePort() {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
