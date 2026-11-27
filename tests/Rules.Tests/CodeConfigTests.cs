using CodeCheck.Slices.CheckCode;

namespace Rules.Tests;

public sealed class CodeConfigTests {
    [Test]
    public void Dotted_keys_and_tables_give_the_same_path() {
        var values = CodeConfig.Parse("umpire.UMP010101.max = 20\n[umpire.UMP010102]\nmax = 3\nsevere = true\n");
        Assert.That(new[] { values.Value("umpire.UMP010101.max"), values.Value("umpire.ump010102.MAX"), values.Value("umpire.UMP010102.severe") },
            Is.EqualTo(new[] { "20", "3", "true" }));
    }

    [Test]
    public void A_table_path_has_no_value() =>
        Assert.That(CodeConfig.Parse("[umpire]\nmax = 1\n").Value("umpire"), Is.Null);

    [Test]
    public void Not_valid_toml_throws_the_config_format_exception() {
        var error = Assert.Catch(() => CodeConfig.Parse("a = 1\n[*.cs]\nb = 2\n"));
        Assert.That(error!.GetType().FullName, Is.EqualTo("Umpire.Config.ConfigFormatException"));
    }
}
