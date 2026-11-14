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
        const Templates.Kind App = Templates.Kind.App, Module = Templates.Kind.Module, Request = Templates.Kind.Request, Command = Templates.Kind.Command,
            Plugin = Templates.Kind.Plugin;
        (string, Templates.Kind)[] expected = [
            ("AGENTS.md.template", App),
            ("App/Host/Program.cs", App),
            ("App/Host/SampleAppApp.cs", App),
            ("App/Views/App.razor", App),
            ("App/Views/Pages/Home.razor", App),
            ("App/Views/Pages/SampleModulePage.razor", Module),
            ("App/Views/Routes.razor", App),
            ("App/Views/_Imports.razor", App),
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
            ("Tests/App.Tests.csproj", App),
            ("Tests/SampleModuleStatusTests.cs", Module),
            ("appsettings.json", App),
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
        Assert.That(files.Single(f => f.Path == "App/Host/Program.cs").Content, Does.Contain("return new HelloApp().Run(args);"));
        Assert.That(files.Single(f => f.Path == "App/Host/HelloApp.cs").Content, Does.Contain("public class HelloApp : VsaApp<VsaDefinition>;"));
    }

    [Test]
    public void NewAppIgnoresTheBuildFolders() {
        Assert.That(NewAppCommand.GitIgnore.Split('\n', StringSplitOptions.TrimEntries), Does.Contain("bin/").And.Contain("obj/"));
    }

    [Test]
    public void NewAppHasOneRootProjectWithTheWebSdkAndVsaSdk() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);
        var projects = files.Where(f => f.Path.EndsWith(".csproj")).Select(f => f.Path).Order(StringComparer.Ordinal);

        Assert.That(projects, Is.EqualTo(["Hello.csproj", "Tests/App.Tests.csproj"]));
        Assert.That(files.Single(f => f.Path == "Hello.csproj").Content,
            Does.StartWith("<Project Sdk=\"Microsoft.NET.Sdk.Web\">\n  <Sdk Name=\"VSA.Sdk\" />\n").And.Not.Contain("Sdk.targets"));
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

        var root = files.Single(f => f.Path == "Hello.csproj").Content;
        Assert.That(root, Does.Contain($"<PackageReference Include=\"VSA.Host\" Version=\"{Version}\" />"));
        Assert.That(root, Does.Contain($"<PackageReference Include=\"VSA.Views\" Version=\"{Version}\" />"));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("$(Vsa"));
    }

    [Test]
    public void NewModuleHasNoProjectAndAPage() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "OrderHistory" }, Version);

        Assert.That(files.Select(f => f.Path), Has.None.EndsWith(".csproj"));
        Assert.That(files.Single(f => f.Path == "Features/OrderHistory/OrderHistoryModule.cs").Content, Does.Contain("namespace OrderHistory;"));
        Assert.That(files.Single(f => f.Path == "App/Views/Pages/OrderHistoryPage.razor").Content, Does.Contain("@page \"/order-history\""));
        Assert.That(files.Single(f => f.Path == "Features/OrderHistory/Domain/Shared/OrderHistoryKey.cs").Content, Does.Contain("namespace OrderHistory.Domain.Shared;"));
        Assert.That(files.Select(f => f.Path + f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewRequestHasRequestResponseAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Request] = "CreateInvoice", [Templates.Module] = "Billing" };
        var files = Templates.Files(Templates.Kind.Request, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Features/Billing/Slices/CreateInvoice/CreateInvoiceHandler.cs",
                        "Features/Billing/Slices/CreateInvoice/CreateInvoiceRequest.cs",
                        "Features/Billing/Slices/CreateInvoice/CreateInvoiceResponse.cs"]));
        Assert.That(string.Concat(files.Select(f => f.Content)), Does.Contain("\"/api/billing/create-invoice\""));
        Assert.That(files.Select(f => f.Content), Has.None.Contains("Sample"));
    }

    [Test]
    public void NewCommandHasCommandResultAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Command] = "ChangeLimit", [Templates.Module] = "Billing" };
        var files = Templates.Files(Templates.Kind.Command, names, Version);

        Assert.That(
            files.Select(f => f.Path).Order(StringComparer.Ordinal),
            Is.EqualTo(["Features/Billing/Slices/ChangeLimit/ChangeLimitCommand.cs",
                        "Features/Billing/Slices/ChangeLimit/ChangeLimitHandler.cs",
                        "Features/Billing/Slices/ChangeLimit/ChangeLimitResult.cs"]));
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
