using Foo.Alpha.Api;
using Foo.Gamma.Domain;

namespace Foo.Gamma.Api;

public sealed class GammaService
{
    private readonly GammaCalculator _calculator = new();
    private readonly GammaFormatter _formatter = new();

    public int Compute(int x) => _calculator.Calculate(x);

    public string Describe(int x) => _formatter.Format(x);

    public int Plugin(int x) => PluginHost.RunByName("Foo.Beta.Api.BetaPlugin, Foo.Beta", "Run", x);
}
