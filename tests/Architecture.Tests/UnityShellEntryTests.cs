namespace EngineCore.Tests;

using BuiltApp.Domain.Shared;
using BuiltApp.Infrastructure;

public sealed class UnityShellEntryTests {
    const string PluginFolder = "Packages/com.vendor.core/Runtime/Plugins";
    const string UnityEngine = "namespace UnityEngine { public class MonoBehaviour { } }";
    const string ShellBase = """
        namespace Vendor.Core {
            using Umpire.Kernels;
            public abstract class Shell<TPack> : UnityEngine.MonoBehaviour, IAppEntry<TPack> where TPack : Pack, new() {
                readonly ShellApp app = new();
                public IApp App => app;
                sealed class ShellApp : App<TPack>;
            }
        }
        """;
    const string GameShell = "namespace Game.App.Host { public sealed class GameShell : Vendor.Core.Shell<Umpire.Vsa.Vsa<Umpire.Kernels.AppHost>> { } }";

    UnityProjectFolder project = null!;
    string editor = null!;
    string scripts = null!;

    [OneTimeSetUp]
    public void CompileAShellOnAUnityEngineBase() {
        project = UnityProjectFolder.Create();
        editor = project.Root + "-editor";
        var managed = Path.Combine(editor, "Data", "Managed", "UnityEngine");
        var plugins = Path.Combine(project.Root, PluginFolder);
        project.Compile("UnityEngine.CoreModule", managed, project.Write("Editor/MonoBehaviour.cs", UnityEngine));
        File.Delete(Path.Combine(project.Root, "Editor", "MonoBehaviour.cs"));
        foreach (var umpire in Directory.GetFiles(AppContext.BaseDirectory, "Umpire*.dll")) {
            Directory.CreateDirectory(plugins);
            File.Copy(umpire, Path.Combine(plugins, Path.GetFileName(umpire)));
        }
        var engine = Path.Combine(managed, "UnityEngine.CoreModule.dll");
        project.CompileWithReferences("Vendor.Core", plugins, [engine], project.Write("Packages/com.vendor.core/Runtime/Shell.cs", ShellBase));
        scripts = Path.Combine(project.Root, "Library", "ScriptAssemblies");
        project.CompileWithReferences("Game", scripts, [engine, Path.Combine(plugins, "Vendor.Core.dll")],
            project.Write("Assets/Game/App/Host/GameShell.cs", GameShell));
        project.Write("Game.csproj", $"<Project><ItemGroup><Reference Include=\"UnityEngine.CoreModule\"><HintPath>{engine}</HintPath></Reference></ItemGroup></Project>");
    }

    [OneTimeTearDown]
    public void DeleteTheProject() {
        project.Dispose();
        Directory.Delete(editor, recursive: true);
    }

    [Test]
    public void A_shell_on_a_Unity_engine_base_is_an_app_class_with_the_architecture_of_its_pack() {
        using var reader = new BinReader(scripts);
        var game = reader.Index().Dlls.Single(d => d.Name == "Game");

        Assert.That(game.AppClasses.Select(a => (a.Type.FullName, a.Architecture?.FullName)),
            Is.EqualTo(new[] { ("Game.App.Host.GameShell", "Umpire.Vsa.Definition.VsaArchitecture") }));
    }

    [Test]
    public void The_references_of_a_Unity_bin_hold_the_package_plugins_and_the_Unity_engine_DLLs() {
        var names = UnityProject.References(scripts).Select(Path.GetFileName);

        Assert.That(names, Does.Contain("Vendor.Core.dll").And.Contain("UnityEngine.CoreModule.dll"));
    }

    [Test]
    public void A_folder_outside_a_Unity_project_has_no_Unity_references() {
        Assert.That(UnityProject.References(AppContext.BaseDirectory), Is.Empty);
    }
}
