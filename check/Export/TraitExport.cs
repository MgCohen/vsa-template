namespace Umpire.CheckModel.Export;

using Api = Umpire.Architecture;

public static class TraitExport {
    public static Trait Of(Api.Trait trait) => trait switch {
        Api.Traits.Static => Plain(TraitKind.Static),
        Api.Traits.Abstract => Plain(TraitKind.Abstract),
        Api.Traits.Sealed => Plain(TraitKind.Sealed),
        Api.Traits.Record => Plain(TraitKind.Record),
        Api.Traits.Struct => Plain(TraitKind.Struct),
        Api.Traits.Interface => Plain(TraitKind.Interface),
        Api.Traits.Enum => Plain(TraitKind.Enum),
        Api.Traits.Delegate => Plain(TraitKind.Delegate),
        Api.AnyOf any => new Trait(TraitKind.AnyOf, [Of(any.Left), Of(any.Right)]),
        Api.AllOf all => new Trait(TraitKind.AllOf, [Of(all.Left), Of(all.Right)]),
        Api.Not not => new Trait(TraitKind.Not, [Of(not.Trait)]),
        _ => throw new NotSupportedException(
            $"The trait {trait.GetType().FullName} has no form as data. Add it to TraitKind, TraitExport and TraitFit."),
    };

    static Trait Plain(TraitKind kind) => new(kind, []);
}
