using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Tests.Shared;

namespace Cli.Tests;

[Category("Package")]
public sealed class UmpireToolTests {
    string root = "";
    Dictionary<string, string> env = [];

    [SetUp]
    public void SetUp() {
        root = TempFolder.Stable(TestContext.CurrentContext.Test.FullName);
        env = new Dictionary<string, string> {
            ["NUGET_PACKAGES"] = LocalFeed.NuGetCache,
            ["DOTNET_CLI_HOME"] = Path.Combine(root, "home"),
        };
    }

    [TearDown]
    public void TearDown() => TempFolder.Delete(root);

    [Test]
    [CancelAfter(600_000)]
    public async Task ScaffoldedAppBuildsAndRuns(CancellationToken ct) {
        var app = NewApp();
        Tool(app, "add", "module", "Billing").EnsureSuccess();
        Tool(app, "add", "request", "CreateInvoice", "-m", "Billing").EnsureSuccess();
        Tool(app, "add", "command", "ChangeLimit", "-m", "Billing").EnsureSuccess();
        Tool(app, "add", "command", "CloseInvoice", "-m", "Billing").EnsureSuccess();
        Assert.That(Directory.GetFiles(Path.Combine(app, "App", "Features"), "*.csproj", SearchOption.AllDirectories), Is.Empty);
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

        Directory.CreateDirectory(Path.Combine(app, "App", "Plugins", "Pipeline"));
        var plugin = Tool(app, "add", "module", "Pipeline");
        Assert.That(plugin.Succeeded, Is.False);
        Assert.That(plugin.Output, Does.Contain("The plugin module Pipeline exists"));
        Directory.Delete(Path.Combine(app, "App", "Plugins", "Pipeline"), recursive: true);

        Tool(app, "add", "plugin", "Game").EnsureSuccess();
        Assert.That(File.Exists(Path.Combine(app, "App", "Plugins", "Game", "GamePlugin.cs")), Is.True);
        Assert.That(File.Exists(Path.Combine(app, "App", "Plugins", "Game", "Contracts", "Commands", "GameStep.cs")), Is.True);
        var pluginAgain = Tool(app, "add", "plugin", "Game");
        Assert.That(pluginAgain.Succeeded, Is.False);
        Assert.That(pluginAgain.Output, Does.Contain("The plugin module Game exists"));
        var pluginOfModule = Tool(app, "add", "plugin", "Billing");
        Assert.That(pluginOfModule.Succeeded, Is.False);
        Assert.That(pluginOfModule.Output, Does.Contain("The module Billing exists"));

        var build = Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();
        Assert.That(build.Output, Does.Contain("umpire-engine: ").And.Contain("0 errors").And.Not.Contain("error UMP"));
        Assert.That(File.Exists(Path.Combine(app, "artifacts~", "obj", "Hello", "release", "umpire", "check", ".umpire-check")), Is.True, build.Output);
        Assert.That(Tool(app, "test", "-c", "Release").EnsureSuccess().Output,
            Does.Contain("failed: 0").And.Match(@"succeeded: [1-9]\d*"));
        await RunAndCall(app, ct, async (client, output) => {
            Assert.That(await client.GetStringAsync("/api/billing/get-status", ct), Does.Contain("\"status\":\"Ready\""));
            var created = await client.PostAsync("/api/billing/create-invoice", null, ct);
            Assert.That(created.IsSuccessStatusCode, Is.True, $"POST /api/billing/create-invoice gave {created.StatusCode}.\n{output}");
            var page = await client.GetStringAsync("/billing", ct);
            Assert.That(page, Does.Contain("<h1>Billing</h1>"));
            Assert.That(page, Does.Contain("Status: Ready"));
        });

        var rootProject = Path.Combine(app, "Hello.csproj");
        File.WriteAllText(rootProject, File.ReadAllText(rootProject).Replace($"Include=\"Umpire.Web\" Version=\"{LocalFeed.Version}\"", "Include=\"Umpire.Web\" Version=\"0.0.1\""));
        var doctor = Tool(app, "doctor");
        Assert.That(doctor.Succeeded, Is.False, doctor.Output);
        Assert.That(doctor.Output, Does.Contain("The versions are not the same"));

        var update = Tool(app, "update", "--version", LocalFeed.Version).EnsureSuccess();
        Assert.That(update.Output, Does.Contain("Hello.csproj"));
        Assert.That(Tool(app, "doctor").EnsureSuccess().Output, Does.Contain($"OK: all use Umpire {LocalFeed.Version}."));
    }

    [Test]
    [CancelAfter(600_000)]
    public async Task NewAppWithNoModuleBuildsOnlyToArtifactsAndRuns(CancellationToken ct) {
        var app = NewApp();

        Dotnet.Run(app, env, "build", "Hello.slnx", "-c", "Release", "-nodeReuse:false").EnsureSuccess();

        var artifacts = Path.Combine(app, "artifacts~");
        Assert.That(File.Exists(Path.Combine(artifacts, "bin", "Hello", "release", "Hello.dll")), Is.True);
        var outputFoldersNextToCode = Directory.EnumerateDirectories(app, "*", SearchOption.AllDirectories)
            .Select(folder => Path.GetRelativePath(app, folder).Replace('\\', '/'))
            .Where(folder => !folder.StartsWith("artifacts~", StringComparison.Ordinal))
            .Where(folder => Path.GetFileName(folder) is "bin" or "obj");
        Assert.That(outputFoldersNextToCode, Is.Empty);
        await RunAndCall(app, ct, async (client, _) =>
            Assert.That(await client.GetStringAsync("/", ct), Does.Contain("<h1>Hello</h1>")));
    }

    [Test]
    [CancelAfter(600_000)]
    public void ADefinitionInTheAppNamesEachLayerWithNoFullName() {
        var app = NewApp("Shop");
        var definition = Directory.CreateDirectory(Path.Combine(app, "Definition")).FullName;
        File.WriteAllText(Path.Combine(definition, "Shop.Definition.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Umpire.Vsa.Definition" Version="{LocalFeed.Version}" PrivateAssets="all" />
              </ItemGroup>
            </Project>

            """);
        File.WriteAllText(Path.Combine(definition, "ShopArchitecture.cs"), """
            namespace Shop.Host;

            using Umpire.Architecture;
            using Umpire.Architecture.Blocks;
            using Umpire.Vsa.Definition;

            public sealed class ShopArchitecture : VsaArchitecture {
                protected override void Rules(RuleBook rules) {
                    rules.For<Host>().Uses<Composition>().And<Kernel>();
                    rules.For<Views>().Uses<Presenters>();
                    rules.For<Feature>().And<Plugin>().Uses<Kernel>();
                    rules.For<Feature>(Depth.Self).Uses<Domain>().And<Domain.Shared>();
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
        var bin = Path.Combine(app, "artifacts~", "bin", "Shop", "debug");
        Assert.That(File.Exists(Path.Combine(bin, "umpire-definitions", "Shop.Definition.dll")), Is.True);
        Assert.That(File.Exists(Path.Combine(bin, "Shop.Definition.dll")), Is.False);
    }

    [Test]
    [CancelAfter(600_000)]
    public void AnAppWithAPackVariantBuilds() => BuildWithAPackVariant(NewApp("Shop"));

    [Test]
    [CancelAfter(600_000)]
    public void ANet8AppWithAPackVariantBuilds() {
        var app = NewApp("Shop");
        File.WriteAllText(Path.Combine(app, "Shop.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net8.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Umpire.Vsa" Version="{LocalFeed.Version}" />
                <PackageReference Include="Umpire.Engine" Version="{LocalFeed.Version}" PrivateAssets="all" />
              </ItemGroup>
            </Project>
            """);
        Directory.Delete(Path.Combine(app, "Views"), recursive: true);
        Directory.Delete(Path.Combine(app, "App", "Tests"), recursive: true);

        BuildWithAPackVariant(app, "Umpire.Kernels", "AppHost");
    }

    void BuildWithAPackVariant(string app, string hostNamespace = "Umpire.Hosts.Web", string host = "WebHost") {
        Tool(app, "add", "pack", "Ecs").EnsureSuccess();
        File.WriteAllText(Path.Combine(app, "App", "ShopApp.cs"), $"""
            namespace Shop.Host;

            using Shop.Packs.Ecs.Kernel;
            using Umpire.Kernels;
            {(hostNamespace == "Umpire.Kernels" ? "" : $"using {hostNamespace};")}

            public class ShopApp : App<EcsKernel, {host}>;
            """);

        Dotnet.Run(app, env, "build", "Shop.csproj", "-nodeReuse:false").EnsureSuccess();
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
