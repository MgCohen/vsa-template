using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1009: a method has at most max lines, from its first line to its last line. Comments and blank lines count.
// The rule checks methods, constructors, finalizers, operators, accessors and local functions.
// Option: vsa.VSA1009.max, in VSA.Rules.globalconfig.
public sealed class MethodLengthRule() : LimitRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1009",
        "A method has at most the maximum number of lines",
        "{0} has {1} lines, and the limit is {2}. Move a part of it to a new method.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxNodeAction(Check,
            SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
            SyntaxKind.OperatorDeclaration, SyntaxKind.ConversionOperatorDeclaration, SyntaxKind.LocalFunctionStatement,
            SyntaxKind.GetAccessorDeclaration, SyntaxKind.SetAccessorDeclaration, SyntaxKind.InitAccessorDeclaration,
            SyntaxKind.AddAccessorDeclaration, SyntaxKind.RemoveAccessorDeclaration);

    void Check(SyntaxNodeAnalysisContext context) {
        var node = context.Node;
        if (Max(context.Options, node.SyntaxTree) is not { } max) {
            return;
        }
        var span = node.GetLocation().GetLineSpan();
        var lines = span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
        if (lines <= max) {
            return;
        }
        var (name, location) = Name(node);
        context.ReportDiagnostic(Create(location, name, lines, max));
    }

    // The name and the location of the diagnostic: the identifier, or the keyword of an operator or an accessor.
    static (string Name, Location Location) Name(SyntaxNode node) => node switch {
        MethodDeclarationSyntax method => (method.Identifier.Text, method.Identifier.GetLocation()),
        ConstructorDeclarationSyntax ctor => ($"Constructor {ctor.Identifier.Text}", ctor.Identifier.GetLocation()),
        DestructorDeclarationSyntax dtor => ($"Finalizer {dtor.Identifier.Text}", dtor.Identifier.GetLocation()),
        OperatorDeclarationSyntax op => ($"Operator {op.OperatorToken.Text}", op.OperatorToken.GetLocation()),
        ConversionOperatorDeclarationSyntax conversion => ($"Conversion to {conversion.Type}", conversion.Type.GetLocation()),
        LocalFunctionStatementSyntax local => (local.Identifier.Text, local.Identifier.GetLocation()),
        AccessorDeclarationSyntax accessor => ($"Accessor {accessor.Keyword.Text}", accessor.Keyword.GetLocation()),
        _ => (node.Kind().ToString(), node.GetLocation()),
    };
}
