using Foo.Gamma.Api;

namespace Foo.Gamma.Tests;

public class GammaServiceTests
{
    [Fact]
    public void Compute_returns_expected_value() => Assert.Equal(132, new GammaService().Compute(2));

    [Fact]
    public void Describe_returns_expected_label() => Assert.Equal("G:g2", new GammaService().Describe(2));

    [Fact]
    public void Plugin_runs_beta_plugin_by_name() => Assert.Equal(14, new GammaService().Plugin(2));
}
