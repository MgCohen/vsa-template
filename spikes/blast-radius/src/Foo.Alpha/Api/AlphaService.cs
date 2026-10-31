using Foo.Alpha.Domain;

namespace Foo.Alpha.Api;

public sealed class AlphaService : IAlphaService
{
    private readonly AlphaCalculator _calculator = new();
    private readonly AlphaFormatter _formatter = new();

    public int Compute(int x) => _calculator.Calculate(x);

    public string Describe(int x) => _formatter.Format(x);

    public AlphaResult Result(int x) => new AlphaResult(Compute(x));

    public int Audit(int x) => x + 3;
}
