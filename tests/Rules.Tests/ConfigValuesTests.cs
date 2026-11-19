using System.Runtime.Loader;
using System.Runtime.CompilerServices;
using Umpire.Vsa.Analyzers;

namespace Rules.Tests;

public sealed class ConfigValuesTests {
    [Test]
    public void Dotted_keys_and_tables_give_the_same_path() {
        var values = ConfigValues.Parse("umpire.UMP010101.max = 20\n[umpire.UMP010102]\nmax = 3\nsevere = true\n");
        Assert.That(new[] { values.Value("umpire.UMP010101.max"), values.Value("umpire.ump010102.MAX"), values.Value("umpire.UMP010102.severe") },
            Is.EqualTo(new[] { "20", "3", "true" }));
    }

    [Test]
    public void A_table_path_has_no_value() =>
        Assert.That(ConfigValues.Parse("[umpire]\nmax = 1\n").Value("umpire"), Is.Null);

    [Test]
    public void Not_valid_toml_throws_the_tomlyn_exception() {
        var error = Assert.Catch(() => ConfigValues.Parse("a = 1\n[*.cs]\nb = 2\n"));
        Assert.That(error!.GetType().FullName, Is.EqualTo("Tomlyn.TomlException"));
    }

    [Test]
    public void The_config_reader_loads_in_a_private_load_context() {
        ConfigValues.Parse("a = 1");
        var contexts = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name == "Umpire.Config")
            .Select(assembly => AssemblyLoadContext.GetLoadContext(assembly)?.Name);
        Assert.That(contexts, Has.Member("Umpire.Config.Private"));
    }
}

public sealed class ConfigReaderLifetimeTests {
    [Test]
    public void The_private_load_context_unloads_with_its_analyzer() {
        var contexts = ReadInUnloadableHostThenUnload();
        for (var i = 0; i < 20 && contexts.Any(context => context.IsAlive); i++) {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        Assert.That(contexts.Select(context => context.IsAlive), Is.EqualTo(new[] { false, false }));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference[] ReadInUnloadableHostThenUnload() {
        var host = new AssemblyLoadContext("AnalyzerHost", isCollectible: true);
        var analyzer = host.LoadFromAssemblyPath(typeof(ConfigValues).Assembly.Location);
        var values = analyzer.GetType(typeof(ConfigValues).FullName!)!.GetMethod(nameof(ConfigValues.Parse))!.Invoke(null, ["a = 1"])!;
        Assert.That(values.GetType().GetMethod(nameof(ConfigValues.Value))!.Invoke(values, ["a"]), Is.EqualTo("1"));
        var privateContext = AssemblyLoadContext.All.Single(context =>
            context.Name == "Umpire.Config.Private" && context.IsCollectible);
        host.Unload();
        return [new WeakReference(host), new WeakReference(privateContext)];
    }
}
