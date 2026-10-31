using Foo.Alpha.Api;

namespace Foo.Alpha.Tests;

public class AlphaServiceTests
{
    [Fact]
    public void Compute_returns_expected_value() => Assert.Equal(21, new AlphaService().Compute(2));

    [Fact]
    public void Describe_returns_expected_label() => Assert.Equal("A:a2", new AlphaService().Describe(2));
}
