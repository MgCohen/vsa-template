namespace Umpire.CheckModel.Analyzer;

using Microsoft.CodeAnalysis;

static class Shapes {
    public static TypeShape Of(ITypeSymbol type) => new(Text(type), [.. Symbols.Named(type).Select(Symbols.NameOf).Distinct()]);

    static string Text(ITypeSymbol type) => type switch {
        IArrayTypeSymbol array => $"{Text(array.ElementType)}[{new string(',', array.Rank - 1)}]",
        IPointerTypeSymbol pointer => Text(pointer.PointedAtType) + "*",
        INamedTypeSymbol { IsAnonymousType: false } named => NamedText(named),
        _ => type.ToDisplayString(),
    };

    static string NamedText(INamedTypeSymbol type) {
        var name = Symbols.NameOf(type).FullName;
        var arguments = Symbols.Arguments(type).ToList();
        return arguments.Count == 0 ? name : $"{name}<{string.Join(",", arguments.Select(Text))}>";
    }
}
