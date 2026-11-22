namespace Umpire.Vsa.Analyzers;

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class BaseLevelCallRule : KernelRule {
    static readonly string[] Levels = ["Configure", "Define", "Rules"];

    static readonly string[] DefinitionBases = ["Umpire.Architecture.Architecture", "Umpire.Architecture.Blocks.Block", "Umpire.Architecture.Concept"];

    public override int Number => 7;

    protected override string Title => "A level of a definition does not call its base level";

    protected override string Message =>
        "{0}.{1} calls base.{1}. The engine calls {1} of each class in the inheritance chain, from the base down, so the base level runs two times. Remove the call to base.{1}.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxNodeAction(Check, SyntaxKind.InvocationExpression);

    void Check(SyntaxNodeAnalysisContext context) {
        var invocation = (InvocationExpressionSyntax)context.Node;
        if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: BaseExpressionSyntax, Name: var name }
            || !Levels.Contains(name.Identifier.Text)) {
            return;
        }
        if (context.ContainingSymbol is not IMethodSymbol { IsOverride: true } method || method.Name != name.Identifier.Text
            || !IsDefinition(method.ContainingType)) {
            return;
        }
        context.ReportDiagnostic(Create(invocation.GetLocation(), method.ContainingType.Name, method.Name));
    }

    static bool IsDefinition(INamedTypeSymbol type) {
        for (var level = type.BaseType; level is not null; level = level.BaseType) {
            if (DefinitionBases.Contains(level.ToDisplayString())) {
                return true;
            }
        }
        return false;
    }
}
