using System.Runtime.Loader;
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
    public void Not_valid_toml_throws_the_config_format_exception() {
        var error = Assert.Catch(() => ConfigValues.Parse("a = 1\n[*.cs]\nb = 2\n"));
        Assert.That(error!.GetType().FullName, Is.EqualTo("Umpire.Config.ConfigFormatException"));
    }

    [Test]
    public void The_kernel_analyzer_embeds_only_the_config_reader() =>
        Assert.That(typeof(ConfigValues).Assembly.GetManifestResourceNames().Where(name => name.StartsWith("UmpireConfigTree/", StringComparison.Ordinal)),
            Is.EqualTo(new[] { "UmpireConfigTree/Umpire.Config.dll" }));

    [Test]
    public void The_config_reader_loads_in_a_private_load_context() {
        ConfigValues.Parse("a = 1");
        var contexts = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name == "Umpire.Config")
            .Select(assembly => AssemblyLoadContext.GetLoadContext(assembly)?.Name);
        Assert.That(contexts, Has.Member("Umpire.Config.Private"));
    }
}
