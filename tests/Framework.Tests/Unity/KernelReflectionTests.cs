namespace Framework.Tests.Unity;

public sealed class KernelReflectionTests {
    [TestCase("Microsoft.Extensions.Configuration.Binder")]
    [TestCase("System.ComponentModel.Annotations")]
    [TestCase("System.ComponentModel.DataAnnotations")]
    public void The_kernel_does_not_reference_a_reflection_binder(string assembly) =>
        Assert.That(BuiltAssembly.OfMultiTarget("Umpire.Kernels", "netstandard2.1").References.Select(reference => reference.Name),
            Has.No.Member(assembly));
}
