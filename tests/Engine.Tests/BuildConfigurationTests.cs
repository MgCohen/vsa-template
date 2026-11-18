using Build.Infrastructure;

namespace Engine.Tests;

public sealed class BuildConfigurationTests {
    [TestCase("C:/app/artifacts~/bin/App/debug", "Debug")]
    [TestCase("C:/app/artifacts~/bin/App/release", "Release")]
    [TestCase("C:/app/artifacts~/bin/App/release_net10.0", "Release")]
    [TestCase("C:/app/bin/Release/net10.0", "Release")]
    [TestCase("C:/app/bin/Debug/net10.0/", "Debug")]
    [TestCase("C:/app/out", null)]
    public void The_build_configuration_comes_from_the_output_folder(string bin, string? configuration) =>
        Assert.That(BuildConfiguration.Of(bin), Is.EqualTo(configuration));
}
