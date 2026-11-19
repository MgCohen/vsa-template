namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class SharedTypeBaseRule : VsaRule {
    public const string AllowedBasesKey = "allowed_bases";

    public override int Number => 7;

    public override bool IsOnByDefault => false;

    public override IReadOnlyList<UmpireOption> Options { get; } = [UmpireOption.List(AllowedBasesKey, [])];

    protected override string Title => "Domain.Shared types have only permitted bases";

    protected override string Message =>
        "{0} has the base {1}. A type in a Domain.Shared project uses only bases from Domain.Shared projects and from the option allowed_bases. Use such a base, or add {2} to umpire.UMP010207.allowed_bases.";

    protected internal override void Register(CompilationStartAnalysisContext start) {
        if (!DomainShared.Is(start.Compilation, start.Options)) {
            return;
        }
        start.RegisterSyntaxNodeAction(Check, SyntaxKind.BaseList);
    }

    void Check(SyntaxNodeAnalysisContext context) {
        if (context.Node.Parent is not TypeDeclarationSyntax declaration
            || context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not { } type) {
            return;
        }
        if (Values(context.Options).List(AllowedBasesKey) is not { } allowed) {
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
        || DomainShared.IsAssembly(baseType.ContainingAssembly?.Name)
        || baseType.ContainingAssembly?.Name == MarkersAssembly;

    const string MarkersAssembly = "Umpire.Markers";

    static string MetadataName(INamedTypeSymbol type) {
        if (type.ContainingType is { } outer) {
            return MetadataName(outer) + "+" + type.MetadataName;
        }
        return type.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() + "." + type.MetadataName : type.MetadataName;
    }
}
