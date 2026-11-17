namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class MemberOrderRule : KernelRule {
    public const string KindOrderKey = "kind_order";
    public const string AccessOrderKey = "access_order";
    public const string StaticFirstKey = "static_first";

    public override int Number => 5;

    public override IReadOnlyList<UmpireOption> Options { get; } =
        [UmpireOption.List(KindOrderKey, ["constant", "field", "constructor", "finalizer", "property", "event", "method", "nested_type"]),
            UmpireOption.List(AccessOrderKey, ["public", "internal", "protected_internal", "protected", "private_protected", "private"]),
            UmpireOption.Bool(StaticFirstKey, true)];

    protected override string Title => "Members are ordered by kind, then by access, then static before instance";

    protected override string Message =>
        "{0} is a {1}, and it comes after a {2}. Move {0} up. The order is: by kind ({3}), then by access ({4}), then {5}.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxNodeAction(Check,
            SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);

    void Check(SyntaxNodeAnalysisContext context) {
        var type = (TypeDeclarationSyntax)context.Node;
        if (Order.Read(Values(context.Options, type.SyntaxTree)) is not { } order) {
            return;
        }
        var inInterface = type is InterfaceDeclarationSyntax;
        MemberGroup? latest = null;
        foreach (var member in type.Members) {
            if (MemberGroup.Of(member, inInterface) is not { } group) {
                continue;
            }
            if (latest is { } previous && order.Compare(group, previous) < 0) {
                var token = Name(member);
                context.ReportDiagnostic(Create(token.GetLocation(), token.Text, group, previous,
                    string.Join(", ", order.Kinds), string.Join(", ", order.Accesses),
                    order.StaticFirst ? "static before instance" : "instance before static"));
                continue;
            }
            latest = group;
        }
    }

    static SyntaxToken Name(MemberDeclarationSyntax member) => member switch {
        FieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
        EventFieldDeclarationSyntax field => field.Declaration.Variables[0].Identifier,
        ConstructorDeclarationSyntax ctor => ctor.Identifier,
        DestructorDeclarationSyntax dtor => dtor.Identifier,
        PropertyDeclarationSyntax property => property.Identifier,
        IndexerDeclarationSyntax indexer => indexer.ThisKeyword,
        EventDeclarationSyntax e => e.Identifier,
        MethodDeclarationSyntax method => method.Identifier,
        OperatorDeclarationSyntax op => op.OperatorToken,
        ConversionOperatorDeclarationSyntax conversion => conversion.OperatorKeyword,
        BaseTypeDeclarationSyntax nested => nested.Identifier,
        DelegateDeclarationSyntax d => d.Identifier,
        _ => member.GetFirstToken(),
    };

    readonly struct MemberGroup(string kind, string access, bool isStatic) {
        public string Kind { get; } = kind;
        public string Access { get; } = access;
        public bool IsStatic { get; } = isStatic;

        public static MemberGroup? Of(MemberDeclarationSyntax member, bool inInterface) {
            string? kind = member switch {
                FieldDeclarationSyntax field => field.Modifiers.Any(SyntaxKind.ConstKeyword) ? "constant" : "field",
                ConstructorDeclarationSyntax => "constructor",
                DestructorDeclarationSyntax => "finalizer",
                EventFieldDeclarationSyntax or EventDeclarationSyntax => "event",
                BasePropertyDeclarationSyntax => "property",
                BaseMethodDeclarationSyntax => "method",
                BaseTypeDeclarationSyntax or DelegateDeclarationSyntax => "nested_type",
                _ => null,
            };
            if (kind is null) {
                return null;
            }
            var isStatic = member.Modifiers.Any(SyntaxKind.StaticKeyword) || member.Modifiers.Any(SyntaxKind.ConstKeyword);
            return new MemberGroup(kind, AccessOf(member, inInterface), isStatic);
        }

        public override string ToString() =>
            $"{Access.Replace('_', ' ')} {(IsStatic && Kind != "constant" ? "static " : "")}{Kind.Replace('_', ' ')}";

        static string AccessOf(MemberDeclarationSyntax member, bool inInterface) {
            var modifiers = member.Modifiers;
            var isProtected = modifiers.Any(SyntaxKind.ProtectedKeyword);
            if (modifiers.Any(SyntaxKind.PublicKeyword) || IsExplicitImplementation(member)) {
                return "public";
            }
            if (modifiers.Any(SyntaxKind.InternalKeyword)) {
                return isProtected ? "protected_internal" : "internal";
            }
            if (isProtected) {
                return modifiers.Any(SyntaxKind.PrivateKeyword) ? "private_protected" : "protected";
            }
            return inInterface && !modifiers.Any(SyntaxKind.PrivateKeyword) ? "public" : "private";
        }

        static bool IsExplicitImplementation(MemberDeclarationSyntax member) => member switch {
            MethodDeclarationSyntax method => method.ExplicitInterfaceSpecifier is not null,
            BasePropertyDeclarationSyntax property => property.ExplicitInterfaceSpecifier is not null,
            _ => false,
        };
    }

    sealed class Order(IReadOnlyList<string> kinds, IReadOnlyList<string> accesses, bool staticFirst) {
        public IReadOnlyList<string> Kinds { get; } = kinds;
        public IReadOnlyList<string> Accesses { get; } = accesses;
        public bool StaticFirst { get; } = staticFirst;

        public static Order? Read(UmpireOptions options) =>
            options.List(KindOrderKey) is { } kinds && options.List(AccessOrderKey) is { } accesses
            && options.Bool(StaticFirstKey) is { } staticFirst
                ? new Order(kinds, accesses, staticFirst)
                : null;

        public int Compare(MemberGroup a, MemberGroup b) {
            var kind = Index(Kinds, a.Kind).CompareTo(Index(Kinds, b.Kind));
            if (kind != 0) {
                return kind;
            }
            var access = Index(Accesses, a.Access).CompareTo(Index(Accesses, b.Access));
            if (access != 0) {
                return access;
            }
            return StaticFirst ? b.IsStatic.CompareTo(a.IsStatic) : a.IsStatic.CompareTo(b.IsStatic);
        }

        static int Index(IReadOnlyList<string> list, string name) {
            var index = list.ToList().IndexOf(name);
            return index < 0 ? list.Count : index;
        }
    }
}
