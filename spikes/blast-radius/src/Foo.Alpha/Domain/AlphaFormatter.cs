using Foo.Alpha.Data;

namespace Foo.Alpha.Domain;

internal sealed class AlphaFormatter
{
    private readonly AlphaStore _store = new();

    internal string Format(int x) => "A:" + _store.Label(x);
}
