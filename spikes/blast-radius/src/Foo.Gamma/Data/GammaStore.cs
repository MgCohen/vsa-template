namespace Foo.Gamma.Data;

internal sealed class GammaStore
{
    internal int Load(int key) => key * 2;

    internal string Label(int key) => "g" + key;
}
