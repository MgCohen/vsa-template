namespace Umpire.Architecture;

public abstract record Trait {
    public static Trait Static => new Traits.Static();
    public static Trait Abstract => new Traits.Abstract();
    public static Trait Sealed => new Traits.Sealed();
    public static Trait Record => new Traits.Record();
    public static Trait Struct => new Traits.Struct();
    public static Trait Interface => new Traits.Interface();
    public static Trait Enum => new Traits.Enum();
    public static Trait Delegate => new Traits.Delegate();

    internal abstract string Text { get; }

    internal abstract bool Fits(TypeFacts type);

    internal virtual string Miss(TypeFacts type) => $"it is not {Text}";

    public sealed override string ToString() => Text;

    public static Trait operator |(Trait left, Trait right) => new AnyOf(left, right);

    public static Trait operator &(Trait left, Trait right) => new AllOf(left, right);

    public static Trait operator !(Trait trait) => new Not(trait);

    internal string Inside<TJoin>() where TJoin : Trait => this is AnyOf or AllOf && this is not TJoin ? $"({Text})" : Text;
}

internal sealed record AnyOf(Trait Left, Trait Right) : Trait {
    internal override string Text => $"{Left.Inside<AnyOf>()} or {Right.Inside<AnyOf>()}";

    internal override bool Fits(TypeFacts type) => Left.Fits(type) || Right.Fits(type);
}

internal sealed record AllOf(Trait Left, Trait Right) : Trait {
    internal override string Text => $"{Left.Inside<AllOf>()} and {Right.Inside<AllOf>()}";

    internal override bool Fits(TypeFacts type) => Left.Fits(type) && Right.Fits(type);

    internal override string Miss(TypeFacts type) => Left.Fits(type) ? Right.Miss(type) : Left.Miss(type);
}

internal sealed record Not(Trait Trait) : Trait {
    internal override string Text => $"not {Trait.Inside<Not>()}";

    internal override bool Fits(TypeFacts type) => !Trait.Fits(type);

    internal override string Miss(TypeFacts type) => $"it is {Trait.Text}";
}
