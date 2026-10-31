using Foo.Beta.Api;

namespace Foo.Beta.Tests;

public class BetaServiceTests
{
    [Fact]
    public void Compute_returns_expected_value() => Assert.Equal(128, new BetaService().Compute(2));

    [Fact]
    public void Describe_returns_expected_label() => Assert.Equal("B:b2", new BetaService().Describe(2));

    [Fact]
    public void Twice_returns_doubled_alpha_value() => Assert.Equal(42, new BetaService().Twice(2));
}
