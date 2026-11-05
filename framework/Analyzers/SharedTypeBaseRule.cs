using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1007: the types of a <Module>.Domain.Shared project have only permitted bases. The rule checks each base type
// and each interface in the base list of a type. A permitted base is in a .Domain.Shared assembly (this project or
// another one), or its metadata name is in the option allowed_bases, for example System.IEquatable`1.
// The rule compares the original definition, so IEquatable<Money> matches System.IEquatable`1.
// Option: vsa.VSA1007.allowed_bases = Card.TypeKeys.TypeKey, System.IEquatable`1. This rule is a trial (decision D-37).
public sealed class SharedTypeBaseRule() : VsaAnalyzerRule(Descriptor) {
    public const string AllowedBasesKey = "allowed_bases";

    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1007",
        "Domain.Shared types have only permitted bases",
        "{0} has the base {1}. A type in a Domain.Shared project uses only bases from Domain.Shared projects and from the option allowed_bases. Use such a base, or add {2} to vsa.VSA1007.allowed_bases.",
        "VSA",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override IReadOnlyList<VsaOption> RequiredOptions { get; } = [VsaOption.List(AllowedBasesKey)];

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSyntaxNodeAction(Check, SyntaxKind.BaseList);
    }

    void Check(SyntaxNodeAnalysisContext context) {
        // An enum has a base list for its underlying type, for example enum Suit : byte.
        if (context.Node.Parent is not TypeDeclarationSyntax declaration
            || context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not { } type) {
            return;
        }
        if (Options(context.Options, context.Node.SyntaxTree).List(AllowedBasesKey) is not { } allowed) {
            return;
        }
        foreach (var baseSyntax in ((BaseListSyntax)context.Node).Types) {
            if (context.SemanticModel.GetTypeInfo(baseSyntax.Type, context.CancellationToken).Type is not INamedTypeSymbol baseType
                || baseType.TypeKind == TypeKind.Error || IsPermitted(baseType, context.Compilation)) {
                continue;
            }
            var name = MetadataName(baseType.OriginalDefinition);
            if (!allowed.Contains(name)) {
                context.ReportDiagnostic(Create(baseSyntax.Type.GetLocation(), type.Name, baseType.ToDisplayString(), name));
            }
        }
    }

    static bool IsPermitted(INamedTypeSymbol baseType, Compilation compilation) =>
        baseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType
        || SymbolEqualityComparer.Default.Equals(baseType.ContainingAssembly, compilation.Assembly)
        || DomainShared.IsAssembly(baseType.ContainingAssembly?.Name);

    // Namespace.Name`N, and Outer+Inner for a nested type.
    static string MetadataName(INamedTypeSymbol type) {
        if (type.ContainingType is { } outer) {
            return MetadataName(outer) + "+" + type.MetadataName;
        }
        return type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() + "." + type.MetadataName : type.MetadataName;
    }
}
