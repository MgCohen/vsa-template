using System.Text.Json.Nodes;
using Tests.Shared;

namespace Rules.Tests;

// The plugin module references of VSA.Sdk, on the projects of tests/Fixtures/PluginApp.
// A plugin module has the layout of a module: its layer projects are in Projects/.
// Each test runs MSBuild on one project: it reads the references, or it runs one check target.
public sealed class SdkPluginTests {
    static string App(params string[] parts) => Repo.Path(["tests", "Fixtures", "PluginApp", .. parts]);

    static string Project(string folder, string name) => App([.. folder.Split('/'), $"{name}.csproj"]);

    // The project references that VSA.Sdk gives, as paths in the fixture app, for example "Plugins/Pipeline/Pipeline.csproj".
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
    public void A_module_layer_gets_the_same_layer_of_each_plugin_module() =>
        Assert.That(References(Project("Modules/UsesPlugins/Projects", "UsesPlugins.Contracts.Requests")), Is.EquivalentTo(new[] {
            "Plugins/Pipeline/Projects/Pipeline.Contracts.Requests.csproj",
            "Plugins/RecordIds/Projects/RecordIds.Contracts.Requests.csproj",
            "Plugins/Units/Projects/Units.Contracts.Requests.csproj",
        }));

    [Test]
    public void A_module_root_gets_each_plugin_root() =>
        Assert.That(References(Project("Modules/UsesPlugins", "UsesPlugins")), Is.SupersetOf(new[] {
            "Plugins/Pipeline/Pipeline.csproj",
            "Plugins/RecordIds/RecordIds.csproj",
            "Plugins/Units/Units.csproj",
        }));

    [Test]
    public void A_module_event_handler_gets_the_events_of_each_plugin_module() =>
        Assert.That(References(Project("Modules/UsesPlugins/Projects", "UsesPlugins.Handlers.Events")),
            Does.Contain("Plugins/Pipeline/Projects/Pipeline.Contracts.Events.csproj"));

    // A plugin base of event handlers handles the events of its own plugin module (Pipeline.Handlers.Events.Audit).
    [Test]
    public void A_plugin_event_handler_gets_the_events_of_its_own_module() =>
        Assert.That(References(Project("Plugins/Pipeline/Projects", "Pipeline.Handlers.Events")),
            Does.Contain("Plugins/Pipeline/Projects/Pipeline.Contracts.Events.csproj"));

    // The fixture writes VsaPluginUses with spaces and a new line.
    [Test]
    public void A_plugin_layer_gets_the_same_layer_of_the_plugin_modules_in_VsaPluginUses() =>
        Assert.That(References(Project("Plugins/Pipeline/Projects", "Pipeline.Contracts.Requests")), Is.EquivalentTo(new[] {
            "Plugins/RecordIds/Projects/RecordIds.Contracts.Requests.csproj",
            "Plugins/Units/Projects/Units.Contracts.Requests.csproj",
        }));

    [Test]
    public void A_plugin_layer_with_no_VsaPluginUses_gets_only_its_own_module() {
        var references = References(Project("Plugins/Pipeline/Projects", "Pipeline.Handlers.Requests"));
        Assert.That(references, Has.All.StartWith("Plugins/Pipeline/"));
        Assert.That(references, Does.Contain("Plugins/Pipeline/Projects/Pipeline.Contracts.Requests.csproj"));
    }

    // A plugin module has the layout of the framework: the folder of a layer, with its subfolders, goes to the project of that layer.
    [Test]
    public void A_plugin_layer_compiles_the_folder_of_its_layer() {
        var result = Dotnet.Run(Repo.Root, null, ["msbuild", Project("Plugins/Pipeline/Projects", "Pipeline.Handlers.Events"), "-getItem:Compile"]).EnsureSuccess();
        var root = App().Replace('\\', '/') + "/";
        var files = JsonNode.Parse(result.Output)!["Items"]!["Compile"]!.AsArray()
            .Select(item => ((string)item!["FullPath"]!).Replace('\\', '/'))
            .Where(path => path.StartsWith(root, StringComparison.Ordinal))
            .Select(path => path[root.Length..]);
        Assert.That(files, Is.EquivalentTo(new[] {
            "Plugins/Pipeline/Handlers/Events/Audit/AuditConfig.cs",
            "Plugins/Pipeline/Handlers/Events/Audit/MessageAudit.cs",
        }));
    }

    [Test]
    public void A_plugin_root_gets_no_module() =>
        Assert.That(References(Project("Plugins/Pipeline", "Pipeline")), Has.None.StartsWith("Modules/"));

    [TestCase("Missing", Description = "no plugin module with this name")]
    [TestCase("Pipeline", Description = "the module itself")]
    [TestCase("../../Modules/UsesPlugins", Description = "a path, not a name")]
    [TestCase("*", Description = "a glob, not a name")]
    public void A_bad_name_in_VsaPluginUses_fails(string name) =>
        Assert.That(Fails(Project("Plugins/Pipeline/Projects", "Pipeline.Contracts.Requests"), "_VsaCheckUses", $"-p:VsaPluginUses={name}"),
            Does.Contain("VSA0002").And.Contain("are not other plugin modules with the layer Contracts/Requests"));

    [Test]
    public void A_plugin_module_with_no_such_layer_in_VsaPluginUses_fails() =>
        Assert.That(Fails(Project("Plugins/Pipeline/Projects", "Pipeline.Handlers.Requests"), "_VsaCheckUses", "-p:VsaPluginUses=RecordIds"),
            Does.Contain("VSA0002").And.Contain(": RecordIds."));

    [Test]
    public void VsaPluginUses_in_a_module_fails() =>
        Assert.That(Fails(Project("Modules/UsesPlugins/Projects", "UsesPlugins.Contracts.Requests"), "_VsaCheckUses", "-p:VsaPluginUses=Pipeline"),
            Does.Contain("VSA0002").And.Contain("is not in a plugin module"));

    // The app root of another app: the project is in a Modules folder, but not in the Modules folder of that root.
    [Test]
    public void A_project_in_a_Modules_folder_out_of_the_app_root_fails() =>
        Assert.That(Fails(Project("Modules/UsesPlugins", "UsesPlugins"), "_VsaCheckRoot", $"-p:VsaAppRoot={Repo.Path("tests", "Fixtures", "Rules")}"),
            Does.Contain("VSA0003"));

    // Shared/ was the folder of the plugin modules. It is now a folder like any other: a project in it gets no rules.
    [Test]
    public void A_project_in_a_Shared_folder_gets_no_plugin_rules() {
        var app = TempFolder.Create();
        try {
            var folder = Path.Combine(app, "Shared", "Old");
            Directory.CreateDirectory(folder);
            var project = Path.Combine(folder, "Old.csproj");
            File.WriteAllText(project, $"""
                <Project>
                  <Import Project="{Repo.Path("samples", "VSA.Sdk.Dev.props")}" />
                  <Import Project="$(VsaSdkDir)Sdk.targets" />
                </Project>
                """);
            var result = Dotnet.Run(Repo.Root, null, ["msbuild", project, "-getProperty:VsaRootFolder", "-getProperty:VsaLayer", $"-p:VsaAppRoot={app}"]).EnsureSuccess();
            var properties = JsonNode.Parse(result.Output)!["Properties"]!;
            Assert.That((string)properties["VsaRootFolder"]!, Is.Empty);
            Assert.That((string)properties["VsaLayer"]!, Is.Empty);
        } finally {
            TempFolder.Delete(app);
        }
    }
}
