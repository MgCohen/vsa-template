using Foo.Alpha.Api;
using Foo.Beta.Data;

namespace Foo.Beta.Domain;

internal sealed class BetaCalculator
{
    private readonly BetaStore _store = new();
    private readonly IAlphaService _upstream;

    internal BetaCalculator(IAlphaService upstream) => _upstream = upstream;

    internal int Calculate(int x) => _store.Load(x) + _upstream.Compute(x) + AlphaLimits.Bonus;
}
