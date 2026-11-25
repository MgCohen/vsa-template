namespace Umpire.CheckModel;

sealed class TraitFit(TypeContent type) {
    public bool Has(Trait trait) => trait.Kind switch {
        TraitKind.Static => type.IsStatic,
        TraitKind.Abstract => type.IsAbstract && type.Kind == TypeKind.Class,
        TraitKind.Sealed => type.IsSealed && type.Kind == TypeKind.Class,
        TraitKind.Record => type.IsRecord && type.Kind == TypeKind.Class,
        TraitKind.Struct => type.Kind == TypeKind.Struct,
        TraitKind.Interface => type.Kind == TypeKind.Interface,
        TraitKind.Enum => type.Kind == TypeKind.Enum,
        TraitKind.Delegate => type.Kind == TypeKind.Delegate,
        TraitKind.AnyOf => trait.Of.Any(Has),
        TraitKind.AllOf => trait.Of.All(Has),
        TraitKind.Not => !Has(trait.Of[0]),
        _ => throw new InvalidOperationException($"The trait {trait.Kind} has no check. Add it to TraitFit."),
    };

    public string Miss(Trait trait) => trait.Kind switch {
        TraitKind.AllOf => Miss(trait.Of.First(t => !Has(t))),
        TraitKind.Not => $"it is {trait.Of[0].Text}",
        _ => $"it is not {trait.Text}",
    };
}
