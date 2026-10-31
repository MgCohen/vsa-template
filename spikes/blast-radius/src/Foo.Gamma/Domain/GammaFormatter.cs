using Foo.Gamma.Data;

namespace Foo.Gamma.Domain;

internal sealed class GammaFormatter
{
    private readonly GammaStore _store = new();

    internal string Format(int x) => "G:" + _store.Label(x);
}
