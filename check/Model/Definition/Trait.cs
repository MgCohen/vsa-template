namespace Umpire.CheckModel;

public sealed record Trait(TraitKind Kind, IReadOnlyList<Trait> Of) {
    public string Text => Kind switch {
        TraitKind.AnyOf => string.Join(" or ", Of.Select(t => t.Inside(Kind))),
        TraitKind.AllOf => string.Join(" and ", Of.Select(t => t.Inside(Kind))),
        TraitKind.Not => $"not {Of[0].Inside(Kind)}",
        _ => Kind.ToString().ToLowerInvariant(),
    };

    public override string ToString() => Text;

    string Inside(TraitKind join) => Kind is TraitKind.AnyOf or TraitKind.AllOf && Kind != join ? $"({Text})" : Text;
}
