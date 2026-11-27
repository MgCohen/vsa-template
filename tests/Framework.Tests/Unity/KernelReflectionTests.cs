namespace Framework.Tests.Unity;

public sealed class KernelReflectionTests {
    [TestCase("Microsoft.Extensions.Configuration.Binder")]
    [TestCase("System.ComponentModel.Annotations")]
    [TestCase("System.ComponentModel.DataAnnotations")]
    public void The_kernel_does_not_reference_a_reflection_binder(string assembly) =>
        Assert.That(BuiltAssembly.OfPortable("Umpire.Kernels").References.Select(reference => reference.Name),
            Has.No.Member(assembly));

    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Configuration")]
    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Configuration.Abstractions")]
    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Configuration.CommandLine")]
    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Logging")]
    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Options")]
    [TestCase("Umpire.Kernels", "Microsoft.Extensions.Primitives")]
    [TestCase("Umpire", "Microsoft.Extensions.Configuration")]
    [TestCase("Umpire", "Microsoft.Extensions.Configuration.Abstractions")]
    [TestCase("Umpire", "Microsoft.Extensions.Configuration.CommandLine")]
    [TestCase("Umpire", "Microsoft.Extensions.Logging")]
    [TestCase("Umpire", "Microsoft.Extensions.Options")]
    [TestCase("Umpire", "Microsoft.Extensions.Primitives")]
    public void The_core_runtime_does_not_reference_a_configuration_or_logging_library(string project, string library) =>
        Assert.That(BuiltAssembly.OfPortable(project).References.Select(reference => reference.Name),
            Has.No.Member(library));
}
