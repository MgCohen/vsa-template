using System.Diagnostics;
using Tests.Shared;
using Umpire.Cli;
using Umpire.Cli.Commands;
using Umpire.Cli.Scaffolding;

namespace Cli.Tests;

public sealed class ScaffoldingTests {
    const string Version = "9.9.9";
    const string GitHubFeed = "https://nuget.pkg.github.com/MgCohen/index.json";

    [TestCase("Billing")]
    [TestCase("OrderHistory2")]
    public void ValidNamesPass(string name) => Assert.That(Names.Check(name, "module"), Is.EqualTo(name));

    [TestCase("billing")]
    [TestCase("Order-History")]
    [TestCase("2Orders")]
    [TestCase("")]
    [TestCase("App")]
    [TestCase("UmpireThings")]
    [TestCase("Features")]
    [TestCase("Home")]
    [TestCase("Pages")]
    [TestCase("Views")]
    [TestCase("System")]
    [TestCase("Microsoft")]
    [TestCase("MySampleModule")]
    [TestCase("SampleActionLog")]
    [TestCase("SampleApp")]
    [TestCase("SampleChangeLog")]
    [TestCase("SamplePluginBase")]
    [TestCase("Plugins")]
    public void InvalidModuleNamesFail(string name) => Assert.Throws<UmpireException>(() => Names.Check(name, "module"));

    [Test]
    public void SourceModePropertyThatStaysIsAToolError() =>
        Assert.That(() => Templates.ToPackageMode("<Project>\n  <PropertyGroup><X>$(UmpireOther)</X></PropertyGroup>\n</Project>\n", Version, "X.csproj"),
            Throws.TypeOf<UmpireException>().With.Message.Contain("X.csproj"));

    [Test]
    public void RequestNamesCannotEndWithRequest() =>
        Assert.That(() => Names.Check("CreateInvoiceRequest", "request", SliceKind.Request.Suffixes), Throws.TypeOf<UmpireException>().With.Message.Contain("CreateInvoice"));

    [TestCase("ChangeLimitCommand")]
    [TestCase("ChangeLimitResult")]
    [TestCase("ChangeLimitHandler")]
    public void CommandNamesCannotEndWithASuffix(string name) =>
        Assert.That(() => Names.Check(name, "command", SliceKind.Command.Suffixes), Throws.TypeOf<UmpireException>().With.Message.Contain("ChangeLimit"));

    [TestCase("Billing", "billing")]
    [TestCase("CreateInvoice", "create-invoice")]
    [TestCase("HTTPServer", "http-server")]
    [TestCase("SampleModule", "sample-module")]
    public void KebabFormIsForRoutes(string name, string kebab) => Assert.That(Names.Kebab(name), Is.EqualTo(kebab));

    [Test]
    public void EachTemplateFileHasOneCommand() {
        const Templates.Kind App = Templates.Kind.App, Module = Templates.Kind.Module, Request = Templates.Kind.Request, Command = Templates.Kind.Command,
            Plugin = Templates.Kind.Plugin;
        (string, Templates.Kind)[] expected = [
            (".gitignore.template", App),
            ("AGENTS.md.template", App),
            ("App/Host/Program.cs", App),
            ("App/Host/SampleAppApp.cs", App),
            ("App/Views/App.razor", App),
            ("App/Views/Pages/Home.razor", App),
            ("App/Views/Pages/SampleModulePage.razor", Module),
            ("App/Views/Routes.razor", App),
            ("App/Views/_Imports.razor", App),
            ("Directory.Build.props.template", App),
            ("Features/.gitkeep", App),
            ("Features/SampleModule/Domain/Shared/SampleModuleKey.cs", Module),
            ("Features/SampleModule/Presenters/SampleModulePresenter.cs", Module),
            ("Features/SampleModule/SampleModuleModule.cs", Module),
            ("Features/SampleModule/Slices/GetStatus/GetStatusHandler.cs", Module),
            ("Features/SampleModule/Slices/GetStatus/GetStatusRequest.cs", Module),
            ("Features/SampleModule/Slices/GetStatus/GetStatusResponse.cs", Module),
            ("Features/SampleModule/Slices/SampleAction/SampleActionHandler.cs", Request),
            ("Features/SampleModule/Slices/SampleAction/SampleActionRequest.cs", Request),
            ("Features/SampleModule/Slices/SampleAction/SampleActionResponse.cs", Request),
            ("Features/SampleModule/Slices/SampleChange/SampleChangeCommand.cs", Command),
            ("Features/SampleModule/Slices/SampleChange/SampleChangeHandler.cs", Command),
            ("Features/SampleModule/Slices/SampleChange/SampleChangeResult.cs", Command),
            ("Plugins/.gitkeep", App),
            ("Plugins/SamplePlugin/Contracts/Commands/SamplePluginStep.cs", Plugin),
            ("Plugins/SamplePlugin/SamplePluginPlugin.cs", Plugin),
            ("Properties/launchSettings.json", App),
            ("SampleApp.csproj", App),
            ("SampleApp.slnx.template", App),
            ("Tests/App.Tests.csproj", App),
            ("Tests/SampleModuleStatusTests.cs", Module),
            ("appsettings.json", App),
            ("dotnet-tools.json.template", App),
            ("global.json.template", App),
            ("nuget.config.template", App),
        ];

        Assert.That(Templates.All().Select(path => (path, Templates.KindOf(path))), Is.EqualTo(expected));
    }

    [Test]
    public void EachTrackedTemplateFileIsInTheTool() {
        var git = Process.Start(new ProcessStartInfo("git", "ls-files") {
            WorkingDirectory = Repo.Path("tools", "cli", "Templates"),
            RedirectStandardOutput = true,
        })!;
        var tracked = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        git.WaitForExit();

        Assert.That(Templates.All(), Is.EqualTo(tracked.Where(f => f is not ("Directory.Build.props" or "Umpire.Sdk.Dev.props")).Order(StringComparer.Ordinal)));
    }

    [Test]
    public void NewAppHasTheAppFilesOnly() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);
        var paths = files.Select(f => f.Path).ToList();

        Assert.That(paths, Does.Contain("AGENTS.md"));
        Assert.That(paths, Does.Contain("App/Host/Program.cs"));
        Assert.That(paths, Does.Contain("App/Views/Pages/Home.razor"));
        Assert.That(paths, Has.None.Contains("Sample"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
        Assert.That(files.Single(f => f.Path == "App/Host/Program.cs").Content, Does.Contain("return new HelloApp().Run(args);"));
        Assert.That(files.Single(f => f.Path == "App/Host/HelloApp.cs").Content, Does.Contain("public class HelloApp : App<Vsa<WebVsaHost>>;"));
        Assert.That(files.Single(f => f.Path == "App/Host/HelloApp.cs").Content, Does.Contain("namespace Hello.App.Host;"));
        Assert.That(files.Single(f => f.Path == "App/Host/Program.cs").Content, Does.Contain("using Hello.App.Host;"));
    }

    [Test]
    public void NewAppIgnoresTheBuildAndLocalFolders() {
        var gitIgnore = NewApp("Hello").Single(f => f.Path == ".gitignore").Content;

        Assert.That(gitIgnore.Split('\n', StringSplitOptions.TrimEntries), Is.SupersetOf(new[] { "artifacts~/", "bin/", "obj/", "TestResults/", "*.user", ".vs/" }));
    }

    [Test]
    public void NewAppWritesItsBuildOutputToArtifacts() {
        var props = NewApp("Hello").Single(f => f.Path == "Directory.Build.props").Content;

        Assert.That(props, Does.Contain("<UseArtifactsOutput>true</UseArtifactsOutput>"));
        Assert.That(props, Does.Contain("<ArtifactsPath>$(MSBuildThisFileDirectory)artifacts~</ArtifactsPath>"));
    }

    [Test]
    public void NewAppWritesTheToolVersionToTheRootFiles() {
        var files = NewApp("Hello");

        Assert.That(files.Single(f => f.Path == "global.json").Content, Does.Contain($"\"Umpire.Sdk\": \"{Version}\""));
        Assert.That(files.Single(f => f.Path == "dotnet-tools.json").Content, Does.Contain($"\"version\": \"{Version}\""));
        Assert.That(files.Single(f => f.Path == "Hello.slnx").Content, Does.Contain("<Project Path=\"Hello.csproj\" />"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains(Templates.VersionToken));
    }

    [Test]
    public void TheRootProjectOfANewAppHasTheBuildPackageOfTheRuleEngine() {
        var files = NewApp("Hello");

        Assert.That(files.Single(f => f.Path == "Hello.csproj").Content,
            Does.Contain($"<PackageReference Include=\"Umpire.Engine.Build\" Version=\"{Version}\" PrivateAssets=\"all\" />"));
        Assert.That(files.Single(f => f.Path == "Tests/App.Tests.csproj").Content, Does.Not.Contain("Umpire.Engine.Build"));
    }

    [Test]
    public void NewAppHasOneRootProjectWithTheWebSdkAndVsaSdk() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);
        var projects = files.Where(f => f.Path.EndsWith(".csproj")).Select(f => f.Path).Order(StringComparer.Ordinal);

        Assert.That(projects, Is.EqualTo(["Hello.csproj", "Tests/App.Tests.csproj"]));
        Assert.That(files.Single(f => f.Path == "Hello.csproj").Content,
            Does.StartWith("<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <Sdk Name=\"Umpire.Sdk\" />\n").And.Not.Contain("Sdk.targets"));
    }

    [Test]
    public void NewAppGetsTheVsaPackagesFromGitHubByDefault() {
        var config = NewApp("Hello").Single(f => f.Path == NugetConfig.FileName).Content;

        Assert.That(config, Does.Contain($"<add key=\"umpire\" value=\"{GitHubFeed}\" />"));
        Assert.That(config, Does.Contain("<add key=\"ClearTextPassword\" value=\"%GITHUB_TOKEN%\" />"));
    }

    [Test]
    public void EachSourceReplacesTheGitHubFeed() {
        var config = NewApp("Hello", "feed", "other").Single(f => f.Path == NugetConfig.FileName).Content;

        Assert.That(config, Does.StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<configuration>"));
        Assert.That(config, Does.Contain($"<add key=\"umpire\" value=\"{Path.GetFullPath("feed")}\" />"));
        Assert.That(config, Does.Contain($"<add key=\"umpire2\" value=\"{Path.GetFullPath("other")}\" />"));
        Assert.That(config, Does.Contain("<add key=\"nuget.org\" value=\"https://api.nuget.org/v3/index.json\" />"));
        Assert.That(config, Does.Not.Contain(GitHubFeed));
        Assert.That(config, Does.Not.Contain("packageSourceCredentials"));
    }

    [Test]
    public void TheTestTemplateUsesThePackageVersionsOfTheRepository() {
        var versions = System.Xml.Linq.XDocument.Load(Repo.Path("Directory.Packages.props")).Descendants("PackageVersion")
            .Where(e => e.Parent!.Attribute("Condition") is null)
            .ToDictionary(e => (string)e.Attribute("Include")!, e => (string)e.Attribute("Version")!);
        var project = System.Xml.Linq.XDocument.Load(Repo.Path("tools", "cli", "Templates", "Tests", "App.Tests.csproj"));

        foreach (var reference in project.Descendants("PackageReference")) {
            var id = (string)reference.Attribute("Include")!;
            Assert.That((string?)reference.Attribute("Version"), Is.EqualTo(versions[id]), id);
        }
    }

    [Test]
    public void NewModuleHasAUnitTest() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "Billing", [Templates.App] = "Shop" }, Version);
        var test = files.Single(f => f.Path == "Tests/BillingStatusTests.cs").Content;

        Assert.That(test, Does.Contain("using Shop.Features.Billing.Slices.GetStatus;"));
        Assert.That(test, Does.Contain("new GetStatusResponse(\"Billing\", \"Ready\")"));
    }

    [Test]
    public void AppProjectsUsePackagesWithTheToolVersion() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);

        var root = files.Single(f => f.Path == "Hello.csproj").Content;
        Assert.That(root, Does.Contain($"<PackageReference Include=\"Umpire.Vsa.Hosts.Web\" Version=\"{Version}\" />"));
        Assert.That(root, Does.Contain($"<PackageReference Include=\"Umpire.Vsa.Views\" Version=\"{Version}\" />"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("$(Umpire"));
    }

    [Test]
    public void NewModuleHasNoProjectAndAPage() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "OrderHistory", [Templates.App] = "Shop" }, Version);

        Assert.That(files.Select(f => f.Path), Has.None.EndsWith(".csproj"));
        Assert.That(files.Single(f => f.Path == "Features/OrderHistory/OrderHistoryModule.cs").Content, Does.Contain("namespace Shop.Features.OrderHistory;"));
        Assert.That(files.Single(f => f.Path == "App/Views/Pages/OrderHistoryPage.razor").Content, Does.Contain("@page \"/order-history\""));
        Assert.That(files.Single(f => f.Path == "Features/OrderHistory/Domain/Shared/OrderHistoryKey.cs").Content, Does.Contain("namespace Shop.Features.OrderHistory.Domain.Shared;"));
        Assert.That(files.Select(f => f.Path + f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewRequestHasRequestResponseAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Request] = "CreateInvoice", [Templates.Module] = "Billing", [Templates.App] = "Shop" };
        var files = Templates.Files(Templates.Kind.Request, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Features/Billing/Slices/CreateInvoice/CreateInvoiceHandler.cs",
                        "Features/Billing/Slices/CreateInvoice/CreateInvoiceRequest.cs",
                        "Features/Billing/Slices/CreateInvoice/CreateInvoiceResponse.cs"]));
        Assert.That(string.Concat(files.Select(f => f.Content)), Does.Contain("\"/api/billing/create-invoice\""));
        Assert.That(files.Select(f => f.Content), Has.All.Contain("namespace Shop.Features.Billing.Slices.CreateInvoice;"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewCommandHasCommandResultAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Command] = "ChangeLimit", [Templates.Module] = "Billing", [Templates.App] = "Shop" };
        var files = Templates.Files(Templates.Kind.Command, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Features/Billing/Slices/ChangeLimit/ChangeLimitCommand.cs",
                        "Features/Billing/Slices/ChangeLimit/ChangeLimitHandler.cs",
                        "Features/Billing/Slices/ChangeLimit/ChangeLimitResult.cs"]));
        Assert.That(files.Single(f => f.Path.EndsWith("ChangeLimitCommand.cs")).Content, Does.Contain("ChangeLimitCommand : ICommand<ChangeLimitResult>"));
        Assert.That(files.Select(f => f.Content), Has.All.Contain("namespace Shop.Features.Billing.Slices.ChangeLimit;"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void WriterDoesNotWriteOverAFile() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "b.txt"), "mine");

            Assert.That(() => FileWriter.WriteAllOrNone(root, [new TemplateFile("a.txt", "new"), new TemplateFile("b.txt", "new")]),
                Throws.TypeOf<UmpireException>().With.Message.Contain("b.txt"));

            Assert.That(File.ReadAllText(Path.Combine(root, "b.txt")), Is.EqualTo("mine"));
            Assert.That(File.Exists(Path.Combine(root, "a.txt")), Is.False);
        } finally {
            TempFolder.Delete(root);
        }
    }

    [Test]
    public void ARequestAndACommandShareOneSliceCommand() {
        var slices = CommandList.All.OfType<AddSliceCommand>().ToList();

        Assert.That(slices.Select(c => c.Usage), Is.EqualTo(new[] { "umpire add request <Name> -m <Module>", "umpire add command <Name> -m <Module>" }));
        Assert.That(slices.Select(c => c.SliceKind), Is.EqualTo(SliceKind.All));
    }

    static IReadOnlyList<TemplateFile> NewApp(string name, params string[] sources) => NewAppCommand.Files(name, sources, Version);
}
