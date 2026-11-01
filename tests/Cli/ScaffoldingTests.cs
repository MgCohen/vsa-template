using Tests.Shared;
using VSA.Cli;
using VSA.Cli.Scaffolding;

namespace Cli.Tests;

// The template engine, the name checks and the file writer, in this process.
public sealed class ScaffoldingTests {
    const string Version = "9.9.9";

    [Theory]
    [InlineData("Billing")]
    [InlineData("OrderHistory2")]
    public void ValidNamesPass(string name) => Assert.Equal(name, Names.Check(name, "module"));

    [Theory]
    [InlineData("billing")]
    [InlineData("Order-History")]
    [InlineData("2Orders")]
    [InlineData("")]
    [InlineData("App")]
    [InlineData("VSAThings")]
    public void InvalidModuleNamesFail(string name) => Assert.Throws<VsaException>(() => Names.Check(name, "module"));

    [Fact]
    public void RequestNamesCannotEndWithRequest() =>
        Assert.Contains("CreateInvoice", Assert.Throws<VsaException>(() => Names.Check("CreateInvoiceRequest", "request")).Message);

    [Theory]
    [InlineData("Billing", "billing")]
    [InlineData("CreateInvoice", "create-invoice")]
    [InlineData("HTTPServer", "http-server")]
    [InlineData("SampleModule", "sample-module")]
    public void KebabFormIsForRoutes(string name, string kebab) => Assert.Equal(kebab, Names.Kebab(name));

    // Each template file and its command. A new template file must be added here, so its command shows in the diff.
    [Fact]
    public void EachTemplateFileHasOneCommand() {
        const Templates.Kind App = Templates.Kind.App, Module = Templates.Kind.Module, Request = Templates.Kind.Request;
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
            ("Modules/SampleModule/Contracts/Events/SampleModule.Contracts.Events.csproj", Module),
            ("Modules/SampleModule/Contracts/Requests/GetStatus/GetStatusRequest.cs", Module),
            ("Modules/SampleModule/Contracts/Requests/GetStatus/GetStatusResponse.cs", Module),
            ("Modules/SampleModule/Contracts/Requests/SampleAction/SampleActionRequest.cs", Request),
            ("Modules/SampleModule/Contracts/Requests/SampleAction/SampleActionResponse.cs", Request),
            ("Modules/SampleModule/Contracts/Requests/SampleModule.Contracts.Requests.csproj", Module),
            ("Modules/SampleModule/Domain/SampleModule.Domain.csproj", Module),
            ("Modules/SampleModule/Handlers/Events/SampleModule.Handlers.Events.csproj", Module),
            ("Modules/SampleModule/Handlers/Requests/GetStatusHandler.cs", Module),
            ("Modules/SampleModule/Handlers/Requests/SampleActionHandler.cs", Request),
            ("Modules/SampleModule/Handlers/Requests/SampleModule.Handlers.Requests.csproj", Module),
            ("Modules/SampleModule/Infrastructure/SampleModule.Infrastructure.csproj", Module),
            ("Modules/SampleModule/Presenters/SampleModule.Presenters.csproj", Module),
            ("Modules/SampleModule/Presenters/SampleModulePresenter.cs", Module),
            ("Modules/SampleModule/SampleModule.csproj", Module),
            ("Modules/SampleModule/SampleModuleModule.cs", Module),
        ];

        Assert.Equal(expected, Templates.All().Select(path => (path, Templates.KindOf(path))));
    }

    [Fact]
    public void NewAppHasTheAppFilesOnly() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);
        var paths = files.Select(f => f.Path).ToList();

        Assert.Contains("AGENTS.md", paths);
        Assert.Contains("App/Host/Program.cs", paths);
        Assert.Contains("App/Views/Pages/Home.razor", paths);
        Assert.DoesNotContain(paths, p => p.Contains("Sample") || p.StartsWith("Modules/Sample"));
        Assert.DoesNotContain(files, f => f.Content.Contains("Sample"));
        Assert.Contains("builder.AddVsa();", files.Single(f => f.Path == "App/Host/Program.cs").Content);
    }

    [Fact]
    public void AppProjectsUsePackagesWithTheToolVersion() {
        var files = Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = "Hello" }, Version);

        Assert.Contains($"<PackageReference Include=\"VSA.Host\" Version=\"{Version}\" />", files.Single(f => f.Path == "App/Host/App.Host.csproj").Content);
        Assert.Contains($"<PackageReference Include=\"VSA.Views\" Version=\"{Version}\" />", files.Single(f => f.Path == "App/Views/App.Views.csproj").Content);
        Assert.DoesNotContain(files, f => f.Content.Contains("$(Vsa"));
    }

    [Fact]
    public void NewModuleHasAllLayersAndAPage() {
        var files = Templates.Files(Templates.Kind.Module, new Dictionary<string, string> { [Templates.Module] = "OrderHistory" }, Version);
        var projects = files.Where(f => f.Path.EndsWith(".csproj")).ToList();

        Assert.Equal(
            ["Modules/OrderHistory/Contracts/Events/OrderHistory.Contracts.Events.csproj",
             "Modules/OrderHistory/Contracts/Requests/OrderHistory.Contracts.Requests.csproj",
             "Modules/OrderHistory/Domain/OrderHistory.Domain.csproj",
             "Modules/OrderHistory/Handlers/Events/OrderHistory.Handlers.Events.csproj",
             "Modules/OrderHistory/Handlers/Requests/OrderHistory.Handlers.Requests.csproj",
             "Modules/OrderHistory/Infrastructure/OrderHistory.Infrastructure.csproj",
             "Modules/OrderHistory/OrderHistory.csproj",
             "Modules/OrderHistory/Presenters/OrderHistory.Presenters.csproj"],
            projects.Select(p => p.Path).Order(StringComparer.Ordinal));
        Assert.All(projects, p => Assert.Equal("<Project Sdk=\"VSA.Sdk\" />\n", p.Content));
        Assert.Contains("@page \"/order-history\"", files.Single(f => f.Path == "App/Views/Pages/OrderHistoryPage.razor").Content);
        Assert.DoesNotContain(files, f => f.Path.Contains("SampleAction") || f.Content.Contains("Sample"));
    }

    [Fact]
    public void NewRequestHasRequestResponseAndHandler() {
        var names = new Dictionary<string, string> { [Templates.Request] = "CreateInvoice", [Templates.Module] = "Billing" };
        var files = Templates.Files(Templates.Kind.Request, names, Version);

        Assert.Equal(
            ["Modules/Billing/Contracts/Requests/CreateInvoice/CreateInvoiceRequest.cs",
             "Modules/Billing/Contracts/Requests/CreateInvoice/CreateInvoiceResponse.cs",
             "Modules/Billing/Handlers/Requests/CreateInvoiceHandler.cs"],
            files.Select(f => f.Path).Order(StringComparer.Ordinal));
        Assert.Contains("\"/api/billing/create-invoice\"", files[0].Content + files[1].Content + files[2].Content);
        Assert.DoesNotContain(files, f => f.Content.Contains("Sample"));
    }

    [Fact]
    public void WriterDoesNotWriteOverAFile() {
        var root = TempFolder.Create();
        try {
            File.WriteAllText(Path.Combine(root, "b.txt"), "mine");

            var error = Assert.Throws<VsaException>(() => FileWriter.Write(root, [("a.txt", "new"), ("b.txt", "new")]));

            Assert.Contains("b.txt", error.Message);
            Assert.Equal("mine", File.ReadAllText(Path.Combine(root, "b.txt")));
            Assert.False(File.Exists(Path.Combine(root, "a.txt")));
        } finally {
            TempFolder.Delete(root);
        }
    }
}
