using System.Diagnostics;
using Tests.Shared;
using VSA.Cli;
using VSA.Cli.Commands;
using VSA.Cli.Scaffolding;

namespace Cli.Tests;

// The template engine, the name checks and the file writer, in this process.
public sealed class ScaffoldingTests {
    const string Version = "9.9.9";

    [TestCase("Billing")]
    [TestCase("OrderHistory2")]
    public void ValidNamesPass(string name) => Assert.That(Names.Check(name, "module"), Is.EqualTo(name));

    [TestCase("billing")]
    [TestCase("Order-History")]
    [TestCase("2Orders")]
    [TestCase("")]
    [TestCase("App")]
    [TestCase("VSAThings")]
    [TestCase("Modules")]
    [TestCase("Home")]
    [TestCase("Pages")]
    [TestCase("Views")]
    [TestCase("System")]
    [TestCase("Microsoft")]
    [TestCase("MySampleModule")]
    [TestCase("SampleActionLog")]
    [TestCase("SampleApp")]
    [TestCase("SampleChangeLog")]
    public void InvalidModuleNamesFail(string name) => Assert.Throws<VsaException>(() => Names.Check(name, "module"));

    [Test]
    public void SourceModePropertyThatStaysIsAToolError() =>
        Assert.That(() => Templates.ToPackageMode("<Project>\n  <PropertyGroup><X>$(VsaOther)</X></PropertyGroup>\n</Project>\n", Version, "X.csproj"),
            Throws.TypeOf<VsaException>().With.Message.Contain("X.csproj"));

    [Test]
    public void RequestNamesCannotEndWithRequest() =>
        Assert.That(() => Names.Check("CreateInvoiceRequest", "request"), Throws.TypeOf<VsaException>().With.Message.Contain("CreateInvoice"));

    [TestCase("ChangeLimitCommand")]
    [TestCase("ChangeLimitResult")]
    [TestCase("ChangeLimitHandler")]
    public void CommandNamesCannotEndWithASuffix(string name) =>
        Assert.That(() => Names.Check(name, "command"), Throws.TypeOf<VsaException>().With.Message.Contain("ChangeLimit"));

    [TestCase("Billing", "billing")]
    [TestCase("CreateInvoice", "create-invoice")]
    [TestCase("HTTPServer", "http-server")]
    [TestCase("SampleModule", "sample-module")]
    public void KebabFormIsForRoutes(string name, string kebab) => Assert.That(Names.Kebab(name), Is.EqualTo(kebab));

    // Each template file and its command. A new template file must be added here, so its command shows in the diff.
    [Test]
    public void EachTemplateFileHasOneCommand() {
        const Templates.Kind App = Templates.Kind.App, Module = Templates.Kind.Module, Request = Templates.Kind.Request, Command = Templates.Kind.Command;
        (string, Templates.Kind)[] expected = [
            ("AGENTS.md.template", App),
            ("App/Host/App.Host.csproj", App),
            ("App/Host/Program.cs", App),
            ("App/Host/Properties/launchSettings.json", App),
            ("App/Host/appsettings.json", App),
            ("App/Views/App.Views.csproj", App),
            ("App/Views/App.razor", App),
            ("App/Views/Pages/Home.razor", App),
            ("App/Views/Pages/SampleModulePage.razor", Module),
            ("App/Views/Routes.razor", App),
            ("App/Views/_Imports.razor", App),
            ("Modules/.gitkeep", App),
            ("Modules/SampleModule/Domain/Shared/SampleModuleKey.cs", Module),
            ("Modules/SampleModule/Presenters/SampleModulePresenter.cs", Module),
            ("Modules/SampleModule/Projects/SampleModule.Contracts.Commands.csproj", Command),
            ("Modules/SampleModule/Projects/SampleModule.Contracts.Events.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Contracts.Requests.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Domain.Shared.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Domain.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Handlers.Commands.csproj", Command),
            ("Modules/SampleModule/Projects/SampleModule.Handlers.Events.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Handlers.Requests.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Infrastructure.csproj", Module),
            ("Modules/SampleModule/Projects/SampleModule.Presenters.csproj", Module),
            ("Modules/SampleModule/SampleModule.csproj", Module),
            ("Modules/SampleModule/SampleModuleModule.cs", Module),
            ("Modules/SampleModule/Slices/GetStatus/GetStatusHandler.cs", Module),
            ("Modules/SampleModule/Slices/GetStatus/GetStatusRequest.cs", Module),
            ("Modules/SampleModule/Slices/GetStatus/GetStatusResponse.cs", Module),
            ("Modules/SampleModule/Slices/SampleAction/SampleActionHandler.cs", Request),
            ("Modules/SampleModule/Slices/SampleAction/SampleActionRequest.cs", Request),
            ("Modules/SampleModule/Slices/SampleAction/SampleActionResponse.cs", Request),
            ("Modules/SampleModule/Slices/SampleChange/SampleChangeCommand.cs", Command),
            ("Modules/SampleModule/Slices/SampleChange/SampleChangeHandler.cs", Command),
            ("Modules/SampleModule/Slices/SampleChange/SampleChangeResult.cs", Command),
            ("Tests/App.Tests.csproj", App),
            ("Tests/SampleModuleStatusTests.cs", Module),
        ];

        Assert.That(Templates.All().Select(path => (path, Templates.KindOf(path))), Is.EqualTo(expected));
    }

    // The embedded resources exclude build output and local files. They must not drop a template file that git tracks.
    // Only the two props files at the top are for source mode.
    [Test]
    public void EachTrackedTemplateFileIsInTheTool() {
        var git = Process.Start(new ProcessStartInfo("git", "ls-files") {
            WorkingDirectory = Repo.Path("framework", "Tooling", "Cli", "Templates"),
            RedirectStandardOutput = true,
        })!;
        var tracked = git.StandardOutput.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        git.WaitForExit();

        Assert.That(Templates.All(), Is.EqualTo(tracked.Where(f => f is not ("Directory.Build.props" or "VSA.Sdk.Dev.props")).Order(StringComparer.Ordinal)));
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
        Assert.That(files.Single(f => f.Path == "App/Host/Program.cs").Content, Does.Contain("VsaBuilder.Create(args).UseWebHost().Build().RunAsync();"));
    }

    [Test]
    public void NewAppIgnoresTheBuildFoldersOfTheLayerProjects() {
        Assert.That(NewAppCommand.GitIgnore.Split('\n', StringSplitOptions.TrimEntries), Does.Contain("bin~/").And.Contain("obj~/"));
    }

    [Test]
    public void NewAppGetsTheVsaPackagesFromGitHubByDefault() {
        var config = NewAppCommand.NugetConfig([]);

        Assert.That(config, Does.Contain($"<add key=\"vsa\" value=\"{NewAppCommand.GitHubFeed}\" />"));
        Assert.That(config, Does.Contain("<add key=\"ClearTextPassword\" value=\"%GITHUB_TOKEN%\" />"));
    }

    [Test]
    public void ASourceReplacesTheGitHubFeed() {
        var config = NewAppCommand.NugetConfig(["feed"]);

        Assert.That(config, Does.Contain($"<add key=\"vsa\" value=\"{Path.GetFullPath("feed")}\" />"));
        Assert.That(config, Does.Not.Contain(NewAppCommand.GitHubFeed));
        Assert.That(config, Does.Not.Contain("packageSourceCredentials"));
    }

    // A new app has no central package versions, so the test template sets each version. Keep them the same as this repository.
    [Test]
    public void TheTestTemplateUsesThePackageVersionsOfTheRepository() {
        var versions = System.Xml.Linq.XDocument.Load(Repo.Path("Directory.Packages.props")).Descendants("PackageVersion")
            .Where(e => e.Parent!.Attribute("Condition") is null)
            .ToDictionary(e => (string)e.Attribute("Include")!, e => (string)e.Attribute("Version")!);
        var project = System.Xml.Linq.XDocument.Load(Repo.Path("framework", "Tooling", "Cli", "Templates", "Tests", "App.Tests.csproj"));

        foreach (var reference in project.Descendants("PackageReference")) {
            var id = (string)reference.Attribute("Include")!;
            Assert.That((string?)reference.Attribute("Version"), Is.EqualTo(versions[id]), id);
        }
    }

    [Test]
    public void NewModuleHasAUnitTest() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "Billing" }, Version);
        var test = files.Single(f => f.Path == "Tests/BillingStatusTests.cs").Content;

        Assert.That(test, Does.Contain("using Billing.Slices.GetStatus;"));
        Assert.That(test, Does.Contain("new GetStatusResponse(\"Billing\", \"Ready\")"));
    }

    [Test]
    public void AppProjectsUsePackagesWithTheToolVersion() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);

        Assert.That(files.Single(f => f.Path == "App/Host/App.Host.csproj").Content, Does.Contain($"<PackageReference Include=\"VSA.Host\" Version=\"{Version}\" />"));
        Assert.That(files.Single(f => f.Path == "App/Views/App.Views.csproj").Content, Does.Contain($"<PackageReference Include=\"VSA.Views\" Version=\"{Version}\" />"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("$(Vsa"));
    }

    [Test]
    public void NewModuleHasAllLayersAndAPage() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "OrderHistory" }, Version);
        var projects = files.Where(f => f.Path.EndsWith(".csproj")).ToList();

        Assert.That(
            projects.Select(p => p.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Modules/OrderHistory/OrderHistory.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Contracts.Events.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Contracts.Requests.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Domain.Shared.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Domain.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Handlers.Events.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Handlers.Requests.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Infrastructure.csproj",
                        "Modules/OrderHistory/Projects/OrderHistory.Presenters.csproj"]));
        Assert.That(projects.Select(p => p.Content), Has.All.EqualTo("<Project Sdk=\"VSA.Sdk\" />\n"));
        Assert.That(files.Single(f => f.Path == "App/Views/Pages/OrderHistoryPage.razor").Content, Does.Contain("@page \"/order-history\""));
        Assert.That(files.Single(f => f.Path == "Modules/OrderHistory/Domain/Shared/OrderHistoryKey.cs").Content, Does.Contain("namespace OrderHistory.Domain.Shared;"));
        Assert.That(files.Select(f => f.Path + f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewRequestHasRequestResponseAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Request] = "CreateInvoice", [Templates.Module] = "Billing" };
        var files = Templates.Files(Templates.Kind.Request, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Modules/Billing/Slices/CreateInvoice/CreateInvoiceHandler.cs",
                        "Modules/Billing/Slices/CreateInvoice/CreateInvoiceRequest.cs",
                        "Modules/Billing/Slices/CreateInvoice/CreateInvoiceResponse.cs"]));
        Assert.That(string.Concat(files.Select(f => f.Content)), Does.Contain("\"/api/billing/create-invoice\""));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewCommandHasCommandResultHandlerAndTheCommandProjects() {
        var names = new Dictionary<string, string> { [Templates.Command] = "ChangeLimit", [Templates.Module] = "Billing" };
        var files = Templates.Files(Templates.Kind.Command, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Modules/Billing/Projects/Billing.Contracts.Commands.csproj",
                        "Modules/Billing/Projects/Billing.Handlers.Commands.csproj",
                        "Modules/Billing/Slices/ChangeLimit/ChangeLimitCommand.cs",
                        "Modules/Billing/Slices/ChangeLimit/ChangeLimitHandler.cs",
                        "Modules/Billing/Slices/ChangeLimit/ChangeLimitResult.cs"]));
        Assert.That(files.Where(f => f.Path.EndsWith(".csproj")).Select(f => f.Content), Has.All.EqualTo("<Project Sdk=\"VSA.Sdk\" />\n"));
        Assert.That(files.Single(f => f.Path.EndsWith("ChangeLimitCommand.cs")).Content, Does.Contain("ChangeLimitCommand : ICommand<ChangeLimitResult>"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void WriterDoesNotWriteOverAFile() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "b.txt"), "mine");

            Assert.That(() => FileWriter.Write(root, [("a.txt", "new"), ("b.txt", "new")]),
                Throws.TypeOf<VsaException>().With.Message.Contain("b.txt"));

            Assert.That(File.ReadAllText(Path.Combine(root, "b.txt")), Is.EqualTo("mine"));
            Assert.That(File.Exists(Path.Combine(root, "a.txt")), Is.False);
        } finally {
            TempFolder.Delete(root);
        }
    }
}
