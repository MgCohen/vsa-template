using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace VSA.Analyzers;

public sealed class CommentRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1013",
        "The code has no comments",
        "The code has a comment. Remove it. Put the information in a name: a type, a member, a variable or a test.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override bool IsModuleRule => false;

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        foreach (var trivia in context.Tree.GetRoot(context.CancellationToken).DescendantTrivia(descendIntoTrivia: true)) {
            if (IsComment(trivia)) {
                context.ReportDiagnostic(Create(Location.Create(context.Tree, SpanWithoutLineBreak(trivia))));
            }
        }
    }

    static bool IsComment(SyntaxTrivia trivia) => trivia.Kind() is
        SyntaxKind.SingleLineCommentTrivia or SyntaxKind.MultiLineCommentTrivia or
        SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia;

    static TextSpan SpanWithoutLineBreak(SyntaxTrivia trivia) {
        var text = trivia.ToFullString();
        return new TextSpan(trivia.FullSpan.Start, text.TrimEnd('\r', '\n').Length);
    }
}
