using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1012: the members of a type are in order: first by kind, then by access, then static before instance.
// The diagnostic marks each member that comes after a member of a later group. An explicit interface implementation is public.
// Options (a kind or an access that a list does not name comes after the named ones):
//   vsa.VSA1012.kind_order   = constant, field, constructor, finalizer, property, event, method, nested_type
//   vsa.VSA1012.access_order = public, internal, protected_internal, protected, private_protected, private
//   vsa.VSA1012.static_first = true
public sealed class MemberOrderRule() : VsaAnalyzerRule(Descriptor) {
    public const string KindOrderKey = "kind_order";
    public const string AccessOrderKey = "access_order";
    public const string StaticFirstKey = "static_first";

    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1012",
        "Members are ordered by kind, then by access, then static before instance",
        "{0} is a {1}, and it comes after a {2}. Move {0} up. The order is: by kind ({3}), then by access ({4}), then {5}.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static IReadOnlyList<string> DefaultKindOrder { get; } =
        ["constant", "field", "constructor", "finalizer", "property", "event", "method", "nested_type"];

    public static IReadOnlyList<string> DefaultAccessOrder { get; } =
        ["public", "internal", "protected_internal", "protected", "private_protected", "private"];

    public static bool DefaultStaticFirst => true;

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxNodeAction(Check,
            SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration,
            SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);

    void Check(SyntaxNodeAnalysisContext context) {
        var type = (TypeDeclarationSyntax)context.Node;
        var order = Order.Read(Options(context.Options, type.SyntaxTree));
        var inInterface = type is InterfaceDeclarationSyntax;
        Group? latest = null;
        foreach (var member in type.Members) {
            if (Group.Of(member, inInterface) is not { } group) {
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

    // The token that names a member, for the location and the message.
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

    // The group of one member: its kind, its access, and static or instance.
    readonly struct Group(string kind, string access, bool isStatic) {
        public string Kind { get; } = kind;
        public string Access { get; } = access;
        public bool IsStatic { get; } = isStatic;

        // Null for a member that the rule does not order.
        public static Group? Of(MemberDeclarationSyntax member, bool inInterface) {
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
            return new Group(kind, AccessOf(member, inInterface), isStatic);
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

    // The order from the options of one file.
    sealed class Order(IReadOnlyList<string> kinds, IReadOnlyList<string> accesses, bool staticFirst) {
        public IReadOnlyList<string> Kinds { get; } = kinds;
        public IReadOnlyList<string> Accesses { get; } = accesses;
        public bool StaticFirst { get; } = staticFirst;

        public static Order Read(VsaOptions options) => new(
            List(options, KindOrderKey, DefaultKindOrder),
            List(options, AccessOrderKey, DefaultAccessOrder),
            options.Get(StaticFirstKey, DefaultStaticFirst));

        public int Compare(Group a, Group b) {
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

        static IReadOnlyList<string> List(VsaOptions options, string key, IReadOnlyList<string> fallback) {
            var items = options.GetList(key);
            return items.Count > 0 ? items : fallback;
        }

        // A name that the list does not have comes after all names of the list.
        static int Index(IReadOnlyList<string> list, string name) {
            var index = list.ToList().IndexOf(name);
            return index < 0 ? list.Count : index;
        }
    }
}
