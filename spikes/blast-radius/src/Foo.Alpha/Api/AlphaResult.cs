namespace Foo.Alpha.Api;

public sealed record AlphaResult(int Value)
{
    public int Doubled => Value * 2;
}
