namespace Foo.Beta.Data;

internal sealed class BetaStore
{
    internal int Load(int key) => key + 100;

    internal string Label(int key) => "b" + key;
}
