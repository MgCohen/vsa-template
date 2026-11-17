using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Tests.Shared;

namespace Cli.Tests;

public sealed class UmpireToolTests {
    string root = "";
    Dictionary<string, string> env = [];

    [SetUp]
    public void SetUp() {
        root = TempFolder.Stable(TestContext.CurrentContext.Test.FullName);
        env = new Dictionary<string, string> {
            ["NUGET_PACKAGES"] = Path.Combine(root, "nuget"),
            ["DOTNET_CLI_HOME"] = Path.Combine(root, "home"),
        };
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task ScaffoldedAppBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();
        Tool(app, "add", "module", "Billing").EnsureSuccess();
        Tool(app, "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();
        Tool(app, "add", "command", "ChangeLimit", "-m", "Billing").EnsureSuccess();
        Tool(app, "add", "command", "CloseInvoice", "-m", "Billing").EnsureSuccess();
        Assert.That(Directory.GetFiles(Path.Combine(app, "Features"), "*.csproj", SearchOption.AllDirectories), Is.Empty);
        Assert.That(Tool(app, "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use Umpire {LocalFeed.Version}."));

        var again = Tool(app, "add", "request", "CreateInvoice", "-m", "Billing");
        Assert.That(again.Succeeded, Is.False);
        Assert.That(again.Output, Does.Contain("does not write over a file"));

        var command = Tool(app, "add", "command", "ChangeLimit", "-m", "Billing");
        Assert.That(command.Succeeded, Is.False);
        Assert.That(command.Output, Does.Contain("does not write over a file"));

        var unknown = Tool(app, "add", "command", "ChangeLimit", "-m", "Shipping");
        Assert.That(unknown.Succeeded, Is.False);
        Assert.That(unknown.Output, Does.Contain("The module Shipping does not exist"));

        Directory.CreateDirectory(Path.Combine(app, "Plugins", "Pipeline"));
        var plugin = Tool(app, "add", "module", "Pipeline");
        Assert.That(plugin.Succeeded, Is.False);
        Assert.That(plugin.Output, Does.Contain("The plugin module Pipeline exists"));
        Directory.Delete(Path.Combine(app, "Plugins", "Pipeline"), recursive: true);

        Tool(app, "add", "plugin", "Game").EnsureSuccess();
        Assert.That(File.Exists(Path.Combine(app, "Plugins", "Game", "GamePlugin.cs")), Is.True);
        Assert.That(File.Exists(Path.Combine(app, "Plugins", "Game", "Contracts", "Commands", "GameStep.cs")), Is.True);
        var pluginAgain = Tool(app, "add", "plugin", "Game");
        Assert.That(pluginAgain.Succeeded, Is.False);
        Assert.That(pluginAgain.Output, Does.Contain("The plugin module Game exists"));
        var pluginOfModule = Tool(app, "add", "plugin", "Billing");
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

        var rootProject = Path.Combine(app, "Hello.csproj");
        File.WriteAllText(rootProject, File.ReadAllText(rootProject).Replace($"Umpire.Sdk.Web/{LocalFeed.Version}", "Umpire.Sdk.Web/0.0.1"));
        var doctor = Tool(app, "doctor");
        Assert.That(doctor.Succeeded, Is.False, doctor.Output);
        Assert.That(doctor.Output, Does.Contain("The versions are not the same"));

        var update = Tool(app, "update", "--version", LocalFeed.Version).EnsureSuccess();
        Assert.That(update.Output, Does.Contain("Hello.csproj"));
        Assert.That(Tool(app, "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use Umpire {LocalFeed.Version}."));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task NewAppWithNoModuleBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();

        Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        await RunAndCall(app, ct, async (client, _) =>
            Assert.That(await client.GetStringAsync("/", ct), Does.Contain("<h1>Hello</h1>")));
    }

    [Test]
    [CancelAfter(600_000)]
    public void ADefinitionInTheAppNamesEachLayerWithNoFullName() {
        var app = NewApp("Shop");
        File.WriteAllText(Path.Combine(app, "App", "Host", "ShopArchitecture.cs"), """
            namespace Shop.App.Host;

            using Umpire.Architecture;
            using Umpire.Architecture.Blocks;
            using Umpire.Vsa;
            using Umpire.Vsa.Definition;

            public sealed class ShopArchitecture : VsaArchitecture {
                protected override void Rules(RuleBook rules) {
                    base.Rules(rules);
                    rules.For<App>().Uses<Kernel>();
                    rules.For<Host>().Uses<Composition>().And<Kernel>();
                    rules.For<Views>().Uses<Presenters>();
                    rules.For<Feature>().And<Plugin>().Uses<Kernel>();
                    rules.For<Module>().Uses<Domain>().And<Domain.Shared>();
                    rules.For<Slice>().Uses<Domain.Shared>();
                    rules.For<Infrastructure>().Uses<Domain>();
                    rules.For<Handlers>().Uses<Contracts>();
                    rules.For<Handlers.Requests>().Uses<Contracts.Requests>();
                    rules.For<Handlers.Events>().Uses<Contracts.Events>();
                    rules.For<Handlers.Commands>().Uses<Contracts.Commands>();
                }
            }
            """);

        Dotnet.Run(app, env, "build", "Shop.slnx", "-nodeReuse:false").EnsureSuccess();
    }

    [Test]
    [CancelAfter(600_000)]
    public void ANewAppWritesItsBuildOutputOnlyToArtifacts() {
        var app = NewApp();

        Dotnet.Run(app, env, "build", "Hello.slnx", "-nodeReuse:false").EnsureSuccess();

        var artifacts = Path.Combine(app, "artifacts~");
        Assert.That(File.Exists(Path.Combine(artifacts, "bin", "Hello", "debug", "Hello.dll")), Is.True);
        var outputFoldersNextToCode = Directory.EnumerateDirectories(app, "*", SearchOption.AllDirectories)
            .Select(folder => Path.GetRelativePath(app, folder).Replace('\\', '/'))
            .Where(folder => !folder.StartsWith("artifacts~", StringComparison.Ordinal))
            .Where(folder => Path.GetFileName(folder) is "bin" or "obj");
        Assert.That(outputFoldersNextToCode, Is.Empty);
    }

    [Test]
    [CancelAfter(600_000)]
    public void NewAppChecksTheArchitecture() {
        var app = NewApp();
        Tool(app, "add", "module", "Billing").EnsureSuccess();
        Tool(app, "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();

        var clean = Dotnet.Run(app, env, "build", "Hello.slnx", "-nodeReuse:false").EnsureSuccess();
        Assert.That(clean.Output, Does.Contain("umpire-engine: ").And.Contain("0 errors").And.Not.Contain("error UMP"));
        Assert.That(Directory.GetFiles(Path.Combine(app, "artifacts~", "obj", "Hello"), "umpire-check.*.stamp", SearchOption.AllDirectories), Is.Not.Empty);

        var probe = Path.Combine(app, "App", "Host", "Probe.cs");
        File.WriteAllText(probe, """
            namespace Hello.App.Host;

            public static class Probe {
                public static Hello.Features.Billing.Slices.CreateInvoice.CreateInvoiceRequest? Request { get; set; }
            }
            """);
        var denied = Dotnet.Run(app, env, "build", "Hello.csproj", "--no-restore", "-nodeReuse:false");
        Assert.That(denied.Succeeded, Is.False, denied.Output);
        Assert.That(denied.Output, Does.Match(Regex.Escape(probe) + @"\(4,\d+\): error UMP020101: Probe in Host"));

        Dotnet.Run(app, env, "build", "Hello.csproj", "--no-restore", "-nodeReuse:false", "-p:UmpireCheck=false").EnsureSuccess();
    }

    string NewApp(string name = "Hello") {
        var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        Tool(root, "new", "app", name, "--source", LocalFeed.Path, "--source", globalPackages).EnsureSuccess();
        return Path.Combine(root, name);
    }

    DotnetResult Tool(string folder, params string[] args) =>
        Dotnet.Run(folder, env, ["tool", "exec", $"Umpire.Cli@{LocalFeed.Version}", "--add-source", LocalFeed.Path, "-y", "--", .. args]);

    async Task RunAndCall(string app, CancellationToken ct, Func<HttpClient, StringBuilder, Task> calls) {
        var url = $"http://127.0.0.1:{FreePort()}";
        var dll = Directory.GetFiles(Path.Combine(app, "artifacts~", "bin", "Hello", "release"), "Hello.dll", SearchOption.AllDirectories).Single();
        using var process = Dotnet.Start(app, env, dll, "--urls", url);
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
