namespace Foo.Alpha.Data;

internal sealed class AlphaStore
{
    internal int Load(int key) => key * 10;

    internal string Label(int key) => "a" + key;
}
