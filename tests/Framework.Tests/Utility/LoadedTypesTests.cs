namespace Framework.Tests.Utility;

using Umpire.Utility;

public sealed class LoadedTypesTests {
    [Test]
    public void The_scan_gives_each_type_of_an_assembly_that_loads() {
        var assembly = typeof(LoadedTypes).Assembly;

        Assert.That(LoadedTypes.Of(assembly), Is.EquivalentTo(assembly.GetTypes()));
    }
}
