namespace Engine.Tests;

using Facts.Infrastructure;

public sealed class SharedAssembliesTests {
    [TestCase("Umpire.Architecture")]
    [TestCase("Umpire.Kernels")]
    [TestCase("umpire.kernels")]
    public void The_definition_and_the_kernel_are_shared_with_the_loaded_app(string assembly) =>
        Assert.That(SharedAssemblies.Contains(assembly), Is.True);

    [Test]
    public void An_app_assembly_is_not_shared() =>
        Assert.That(SharedAssemblies.Contains("Notes"), Is.False);
}
