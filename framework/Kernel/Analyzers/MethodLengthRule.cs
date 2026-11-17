namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class MethodLengthRule : LimitRule {
    public override int Number => 2;

    protected override int DefaultMax => 40;

    protected override string Title => "A method has at most the maximum number of lines";

    protected override string Message =>
        "{0} has {1} lines, and the limit is {2}. Move a part of it to a new method.";

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
        var named = Name(node);
        context.ReportDiagnostic(Create(named.Location, named.Name, lines, max));
    }

    static NamedNode Name(SyntaxNode node) => node switch {
        MethodDeclarationSyntax method => new(method.Identifier.Text, method.Identifier.GetLocation()),
        ConstructorDeclarationSyntax ctor => new($"Constructor {ctor.Identifier.Text}", ctor.Identifier.GetLocation()),
        DestructorDeclarationSyntax dtor => new($"Finalizer {dtor.Identifier.Text}", dtor.Identifier.GetLocation()),
        OperatorDeclarationSyntax op => new($"Operator {op.OperatorToken.Text}", op.OperatorToken.GetLocation()),
        ConversionOperatorDeclarationSyntax conversion => new($"Conversion to {conversion.Type}", conversion.Type.GetLocation()),
        LocalFunctionStatementSyntax local => new(local.Identifier.Text, local.Identifier.GetLocation()),
        AccessorDeclarationSyntax accessor => new($"Accessor {accessor.Keyword.Text}", accessor.Keyword.GetLocation()),
        _ => new(node.Kind().ToString(), node.GetLocation()),
    };

    sealed class NamedNode(string name, Location location) {
        public string Name { get; } = name;

        public Location Location { get; } = location;
    }
}
