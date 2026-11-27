namespace UmpireEngine;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using FactKind = Umpire.CheckModel.TypeKind;
using Umpire.CheckModel;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

sealed class ContentReader {
    readonly INamedTypeSymbol type;
    readonly SourcePlaces places;
    readonly ILookup<ISymbol, TypeName> implements;

    public ContentReader(INamedTypeSymbol type, SourcePlaces places) {
        this.type = type;
        this.places = places;
        implements = Implemented().ToLookup(i => i.Member, i => i.Interface, SymbolEqualityComparer.Default);
    }

    public TypeContent Read(string file, bool isFirst) => new(
        Kind, Symbols.AccessOf(type), type.IsAbstract && !type.IsStatic, type.IsStatic, type.IsSealed && !type.IsStatic, type.IsRecord,
        IsPartial, type.IsReadOnly, type.IsRefLikeType,
        [.. type.TypeParameters.Select(MemberReader.TypeParameter)],
        [.. type.Interfaces.Select(Shapes.Of)],
        [.. type.AllInterfaces.Select(Shapes.Of)],
        [.. Bases().Select(Symbols.NameOf).Distinct()],
        [.. BaseArguments().Select(Symbols.NameOf).Distinct()],
        [.. Attributes(file, isFirst)],
        [.. MemberReader.MembersOf(type).Where(m => IsIn(m, file, isFirst)).Select(Member)],
        [.. MutableMembers.Of(type)],
        type.BaseType is { SpecialType: SpecialType.None } baseType ? Shapes.Of(baseType) : null);

    FactKind Kind => type.TypeKind switch {
        TypeKind.Struct => FactKind.Struct,
        TypeKind.Interface => FactKind.Interface,
        TypeKind.Enum => FactKind.Enum,
        TypeKind.Delegate => FactKind.Delegate,
        _ => FactKind.Class,
    };

    bool IsPartial => type.DeclaringSyntaxReferences.Select(r => r.GetSyntax())
        .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax>()
        .Any(s => s.Modifiers.Any(SyntaxKind.PartialKeyword));

    MemberFact Member(ISymbol member) => MemberReader.Fact(
        member, [.. implements[member].Distinct()], places.PositionOf(member.Locations.Concat(type.Locations).First()).Line);

    IEnumerable<AttributeFact> Attributes(string file, bool isFirst) => AttributeReader.Of(type)
        .Where(a => a.Data.AttributeClass is { TypeKind: not TypeKind.Error } && IsIn(a.Data, file, isFirst))
        .Select(AttributeReader.Fact);

    bool IsIn(AttributeData attribute, string file, bool isFirst) =>
        attribute.ApplicationSyntaxReference is { } reference ? places.FileOf(reference.SyntaxTree) == file : isFirst;

    bool IsIn(ISymbol member, string file, bool isFirst) =>
        member.Locations.FirstOrDefault(l => l.IsInSource) is { SourceTree: { } tree } && !member.IsImplicitlyDeclared
            ? places.FileOf(tree) == file
            : isFirst;

    IEnumerable<INamedTypeSymbol> Bases() => new[] { type }.Concat(Symbols.BasesOf(type)).Concat(Root()).Concat(type.AllInterfaces);

    IEnumerable<INamedTypeSymbol> Root() =>
        type.TypeKind is TypeKind.Class or TypeKind.Delegate && Symbols.BasesOf(type).LastOrDefault() is var last
            && (last ?? type).BaseType is { SpecialType: SpecialType.System_Object } root
            ? [root]
            : [];

    IEnumerable<INamedTypeSymbol> BaseArguments() => Symbols.BasesOf(type).Concat(type.AllInterfaces)
        .SelectMany(b => b.TypeArguments).SelectMany(a => Symbols.Named(a).Take(1));

    IEnumerable<Implementation> Implemented() {
        if (type.TypeKind == TypeKind.Interface) {
            yield break;
        }
        foreach (var declared in type.AllInterfaces) {
            foreach (var member in declared.GetMembers()) {
                if (type.FindImplementationForInterfaceMember(member) is { } own
                    && SymbolEqualityComparer.Default.Equals(own.ContainingType, type)) {
                    yield return new Implementation(own, Symbols.NameOf(declared));
                }
            }
        }
    }

    sealed record Implementation(ISymbol Member, TypeName Interface);
}
