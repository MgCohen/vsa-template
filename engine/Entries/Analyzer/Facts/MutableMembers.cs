namespace UmpireEngine;

using Microsoft.CodeAnalysis;
using Umpire.CheckModel;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

static class MutableMembers {
    const string CompilerName = "<";

    public static IEnumerable<string> Of(INamedTypeSymbol type) => type.TypeKind == TypeKind.Enum
        ? []
        : type.GetMembers().Where(IsMutableField).Select(m => m.Name)
            .Concat(type.GetMembers().OfType<IPropertySymbol>().Where(IsSettable).Select(p => p.MetadataName))
            .Distinct();

    static bool IsMutableField(ISymbol member) => member switch {
        IFieldSymbol field => field is { IsReadOnly: false, IsConst: false, AssociatedSymbol: null } && !IsCompilerMade(field),
        IEventSymbol e => e is { IsAbstract: false, AddMethod.IsImplicitlyDeclared: true },
        _ => false,
    };

    static bool IsSettable(IPropertySymbol property) => property.SetMethod is { IsInitOnly: false };

    static bool IsCompilerMade(IFieldSymbol field) => field.Name.StartsWith(CompilerName, StringComparison.Ordinal);
}
