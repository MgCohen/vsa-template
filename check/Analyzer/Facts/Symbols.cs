namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;

static class Symbols {
    public static TypeName NameOf(INamedTypeSymbol type) {
        var definition = (IsExtensionBlock(type) ? type.ContainingType : type).OriginalDefinition;
        return new TypeName(FullName(definition), definition.ContainingAssembly?.Name ?? "");
    }

    public static IEnumerable<INamedTypeSymbol> TypesOf(INamespaceSymbol space) =>
        space.GetNamespaceMembers().SelectMany(TypesOf).Concat(space.GetTypeMembers().SelectMany(WithNested));

    static bool IsExtensionBlock(INamedTypeSymbol type) => type.Name.Length == 0 && type.ContainingType is not null;

    public static IEnumerable<INamedTypeSymbol> Named(ITypeSymbol? type) {
        switch (type) {
            case IArrayTypeSymbol array:
                return Named(array.ElementType);
            case IPointerTypeSymbol pointer:
                return Named(pointer.PointedAtType);
            case IFunctionPointerTypeSymbol function:
                return Named(function.Signature.ReturnType).Concat(function.Signature.Parameters.SelectMany(p => Named(p.Type)));
            case INamedTypeSymbol { IsAnonymousType: true } anonymous:
                return anonymous.GetMembers().OfType<IPropertySymbol>().SelectMany(p => Named(p.Type));
            case INamedTypeSymbol { TypeKind: not TypeKind.Error } named:
                return new[] { named }.Concat(Arguments(named).SelectMany(Named));
            default:
                return [];
        }
    }

    public static IEnumerable<ITypeSymbol> Arguments(INamedTypeSymbol type) =>
        type.ContainingType is { } outer ? Arguments(outer).Concat(type.TypeArguments) : type.TypeArguments;

    public static IEnumerable<INamedTypeSymbol> BasesOf(INamedTypeSymbol type) {
        for (var current = type.BaseType; current is { SpecialType: not SpecialType.System_Object }; current = current.BaseType) {
            yield return current;
        }
    }

    public static MemberAccess AccessOf(ISymbol symbol) => symbol.DeclaredAccessibility switch {
        Accessibility.Public => MemberAccess.Public,
        Accessibility.Protected => MemberAccess.Protected,
        Accessibility.ProtectedOrInternal => MemberAccess.ProtectedInternal,
        Accessibility.Internal => MemberAccess.Internal,
        Accessibility.ProtectedAndInternal => MemberAccess.PrivateProtected,
        _ => MemberAccess.Private,
    };

    static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type) =>
        new[] { type }.Concat(type.GetTypeMembers().Where(t => !IsExtensionBlock(t)).SelectMany(WithNested));

    static string FullName(INamedTypeSymbol type) {
        if (type.ContainingType is { } outer) {
            return $"{FullName(outer)}+{type.MetadataName}";
        }
        return type.ContainingNamespace is { IsGlobalNamespace: false } space
            ? $"{space.ToDisplayString()}.{type.MetadataName}"
            : type.MetadataName;
    }
}
