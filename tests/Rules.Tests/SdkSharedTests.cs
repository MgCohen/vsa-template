using System.Text.Json.Nodes;
using Tests.Shared;

namespace Rules.Tests;

// The Shared module references of VSA.Sdk, on the projects of tests/Fixtures/SharedApp.
// Each test runs MSBuild on one project: it reads the references, or it runs one check target.
public sealed class SdkSharedTests {
    static string App(params string[] parts) => Repo.Path(["tests", "Fixtures", "SharedApp", .. parts]);

    static string Project(string folder, string name) => App([.. folder.Split('/'), $"{name}.csproj"]);

    // The project references that VSA.Sdk gives, as paths in the fixture app, for example "Shared/Pipeline/Pipeline.csproj".
    static IReadOnlyList<string> References(string project, params string[] properties) {
        var result = Dotnet.Run(Repo.Root, null, ["msbuild", project, "-getItem:ProjectReference", .. properties]).EnsureSuccess();
        var root = App().Replace('\\', '/') + "/";
        return [.. JsonNode.Parse(result.Output)!["Items"]!["ProjectReference"]!.AsArray()
            .Select(item => ((string)item!["Identity"]!).Replace('\\', '/'))
            .Where(path => path.StartsWith(root, StringComparison.Ordinal))
            .Select(path => path[root.Length..])];
    }

    // The output of one check target. The check fails the run.
    static string Fails(string project, string target, params string[] properties) {
        var result = Dotnet.Run(Repo.Root, null, ["msbuild", project, $"-t:{target}", "-nologo", .. properties]);
        Assert.That(result.Succeeded, Is.False, result.Output);
        return result.Output;
    }

    [Test]
    public void A_module_layer_gets_the_same_layer_of_each_Shared_module() =>
        Assert.That(References(Project("Modules/UsesShared/Contracts/Requests", "UsesShared.Contracts.Requests")), Is.EquivalentTo(new[] {
            "Shared/Pipeline/Contracts/Requests/Pipeline.Contracts.Requests.csproj",
            "Shared/RecordIds/Contracts/Requests/RecordIds.Contracts.Requests.csproj",
            "Shared/Units/Contracts/Requests/Units.Contracts.Requests.csproj",
        }));

    [Test]
    public void A_module_root_gets_each_Shared_root() =>
        Assert.That(References(Project("Modules/UsesShared", "UsesShared")), Is.SupersetOf(new[] {
            "Shared/Pipeline/Pipeline.csproj",
            "Shared/RecordIds/RecordIds.csproj",
            "Shared/Units/Units.csproj",
        }));

    [Test]
    public void A_module_event_handler_gets_the_events_of_each_Shared_module() =>
        Assert.That(References(Project("Modules/UsesShared/Handlers/Events", "UsesShared.Handlers.Events")),
            Does.Contain("Shared/Pipeline/Contracts/Events/Pipeline.Contracts.Events.csproj"));

    // The fixture writes VsaUses with spaces and a new line.
    [Test]
    public void A_Shared_layer_gets_the_same_layer_of_the_Shared_modules_in_VsaUses() =>
        Assert.That(References(Project("Shared/Pipeline/Contracts/Requests", "Pipeline.Contracts.Requests")), Is.EquivalentTo(new[] {
            "Shared/RecordIds/Contracts/Requests/RecordIds.Contracts.Requests.csproj",
            "Shared/Units/Contracts/Requests/Units.Contracts.Requests.csproj",
        }));

    [Test]
    public void A_Shared_layer_with_no_VsaUses_gets_only_its_own_module() {
        var references = References(Project("Shared/Pipeline/Handlers/Requests", "Pipeline.Handlers.Requests"));
        Assert.That(references, Has.All.StartWith("Shared/Pipeline/"));
        Assert.That(references, Does.Contain("Shared/Pipeline/Contracts/Requests/Pipeline.Contracts.Requests.csproj"));
    }

    [Test]
    public void A_Shared_root_gets_no_module() =>
        Assert.That(References(Project("Shared/Pipeline", "Pipeline")), Has.None.StartsWith("Modules/"));

    [TestCase("Missing", Description = "no Shared module with this name")]
    [TestCase("Pipeline", Description = "the module itself")]
    [TestCase("../../Modules/UsesShared", Description = "a path, not a name")]
    [TestCase("*", Description = "a glob, not a name")]
    public void A_bad_name_in_VsaUses_fails(string name) =>
        Assert.That(Fails(Project("Shared/Pipeline/Contracts/Requests", "Pipeline.Contracts.Requests"), "_VsaCheckUses", $"-p:VsaUses={name}"),
            Does.Contain("VSA0002").And.Contain("are not other Shared modules with the layer Contracts/Requests"));

    [Test]
    public void A_Shared_module_with_no_such_layer_in_VsaUses_fails() =>
        Assert.That(Fails(Project("Shared/Pipeline/Handlers/Requests", "Pipeline.Handlers.Requests"), "_VsaCheckUses", "-p:VsaUses=RecordIds"),
            Does.Contain("VSA0002").And.Contain(": RecordIds."));

    [Test]
    public void VsaUses_in_a_module_fails() =>
        Assert.That(Fails(Project("Modules/UsesShared/Contracts/Requests", "UsesShared.Contracts.Requests"), "_VsaCheckUses", "-p:VsaUses=Pipeline"),
            Does.Contain("VSA0002").And.Contain("is not in a Shared module"));

    // The app root of another app: the project is in a Modules folder, but not in the Modules folder of that root.
    [Test]
    public void A_project_in_a_Modules_folder_out_of_the_app_root_fails() =>
        Assert.That(Fails(Project("Modules/UsesShared", "UsesShared"), "_VsaCheckRoot", $"-p:VsaAppRoot={Repo.Path("tests", "Fixtures", "Rules")}"),
            Does.Contain("VSA0003"));
}
