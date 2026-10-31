using Foo.Gamma.Data;

namespace Foo.Gamma.Domain;

internal sealed class GammaCalculator
{
    private readonly GammaStore _store = new();
    private readonly Foo.Beta.Api.BetaService _upstream = new();

    internal int Calculate(int x) => _store.Load(x) + _upstream.Compute(x);
}
