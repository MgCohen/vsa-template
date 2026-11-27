namespace UmpireEngine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Umpire.CheckModel;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

sealed record OwnedAttribute(AttributeData Data, string? Member);

static class AttributeReader {
    public static IEnumerable<OwnedAttribute> Of(INamedTypeSymbol type) {
        var own = type.GetAttributes()
            .Concat(type.TypeParameters.SelectMany(p => p.GetAttributes()))
            .Concat(type.DelegateInvokeMethod is { } invoke ? OfMember(invoke) : [])
            .Select(a => new OwnedAttribute(a, null));
        return own.Concat(type.GetMembers().Where(m => m is not INamedTypeSymbol && !(m.IsImplicitlyDeclared && type.TypeKind == TypeKind.Delegate))
            .SelectMany(m => OfMember(m).Select(a => new OwnedAttribute(a, NameOf(m)))));
    }

    public static AttributeFact Fact(OwnedAttribute attribute) {
        var type = attribute.Data.AttributeClass!;
        return new AttributeFact(
            Symbols.NameOf(type),
            [.. new[] { type }.Concat(Symbols.BasesOf(type)).Select(Symbols.NameOf)],
            [.. attribute.Data.ConstructorArguments.Select(v => v.ToCSharpString()),
                .. attribute.Data.NamedArguments.Select(v => $"{v.Key} = {v.Value.ToCSharpString()}")],
            attribute.Member);
    }

    static string NameOf(ISymbol member) => member switch {
        IFieldSymbol { AssociatedSymbol: { } owner } => owner.Name,
        IMethodSymbol { AssociatedSymbol: { } owner } => owner.Name,
        _ => member.Name,
    };

    static IEnumerable<AttributeData> OfMember(ISymbol member) {
        var method = member as IMethodSymbol;
        var parameters = method?.Parameters ?? (member as IPropertySymbol)?.Parameters ?? [];
        return member.GetAttributes()
            .Concat(method?.GetReturnTypeAttributes() ?? [])
            .Concat(method?.TypeParameters.SelectMany(p => p.GetAttributes()) ?? [])
            .Concat(parameters.SelectMany(p => p.GetAttributes()));
    }
}
