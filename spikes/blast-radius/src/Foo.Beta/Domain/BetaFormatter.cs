using Foo.Beta.Data;

namespace Foo.Beta.Domain;

internal sealed class BetaFormatter
{
    private readonly BetaStore _store = new();

    internal string Format(int x) => "B:" + _store.Label(x);
}
