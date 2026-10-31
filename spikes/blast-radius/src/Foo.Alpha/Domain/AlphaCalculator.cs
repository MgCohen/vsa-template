using Foo.Alpha.Data;

namespace Foo.Alpha.Domain;

internal sealed class AlphaCalculator
{
    private readonly AlphaStore _store = new();


    internal int Calculate(int x) => _store.Load(x) + 1;
}
