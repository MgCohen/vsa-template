namespace UmpireEngine;

using Facts.Domain.Shared.Compile;
using Microsoft.CodeAnalysis;

static class CodeTypeReader {
    public static CodeType Read(INamedTypeSymbol type, INamedTypeSymbol? request, Compilation compilation, Func<SyntaxTree, bool> isGenerated) {
        var name = Symbols.NameOf(type);
        return new CodeType(
            name.FullName, name.Assembly, NameOf(type), type.IsAbstract, type.TypeKind is TypeKind.Class or TypeKind.Struct, type.IsRecord,
            request is null ? null : RequestOf(type, request, compilation, isGenerated));
    }

    static CodeRequest? RequestOf(INamedTypeSymbol type, INamedTypeSymbol request, Compilation compilation, Func<SyntaxTree, bool> isGenerated) {
        var implemented = type.AllInterfaces.FirstOrDefault(i => SymbolEqualityComparer.Default.Equals(i.OriginalDefinition, request));
        if (implemented is null) {
            return null;
        }
        var hasConcreteBase = type.BaseType is { IsAbstract: false } baseType
            && baseType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, implemented));
        var response = implemented.TypeArguments[0] is INamedTypeSymbol given && given.Locations.Any(l => l.IsInSource)
            && SymbolEqualityComparer.Default.Equals(given.ContainingAssembly, compilation.Assembly)
            && !(given.Locations[0].SourceTree is { } tree && isGenerated(tree))
                ? new CodeResponse(NameOf(given), given.IsRecord)
                : null;
        return new CodeRequest(hasConcreteBase, response);
    }

    static CodeName NameOf(INamedTypeSymbol type) => new(type.Name, CodePlaces.Of(type.Locations[0]));
}
