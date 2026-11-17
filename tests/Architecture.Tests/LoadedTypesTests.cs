namespace EngineCore.Tests;

using Umpire.Architecture;

public sealed class LoadedTypesTests {
    [Test]
    public void The_scan_gives_each_type_of_an_assembly_that_loads() {
        var assembly = typeof(LoadedTypes).Assembly;

        Assert.That(LoadedTypes.Of(assembly), Is.EquivalentTo(assembly.GetTypes()));
    }

    [TestCase("<>c", true)]
    [TestCase("<Main>$", true)]
    [TestCase("Invoice", false)]
    public void A_type_name_that_starts_with_an_angle_bracket_is_compiler_generated(string name, bool generated) {
        Assert.That(LoadedTypes.IsCompilerGenerated(name), Is.EqualTo(generated));
    }
}
