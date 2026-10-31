using Foo.Alpha.Api;
using Foo.Beta.Domain;

namespace Foo.Beta.Api;

public sealed class BetaService
{
    private readonly BetaCalculator _calculator;
    private readonly BetaFormatter _formatter = new();

    public BetaService() : this(new AlphaService()) { }

    public BetaService(IAlphaService upstream) => _calculator = new BetaCalculator(upstream);

    public int Compute(int x) => _calculator.Calculate(x);

    public string Describe(int x) => _formatter.Format(x);

    public int Twice(int x) => new AlphaService().Result(x).Doubled;
}
