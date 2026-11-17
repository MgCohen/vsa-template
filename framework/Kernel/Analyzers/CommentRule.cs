namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

public sealed class CommentRule : KernelRule {
    public override int Number => 6;

    protected override string Title => "The code has no comments";

    protected override string Message =>
        "The code has a comment. Remove it. Put the information in a name: a type, a member, a variable or a test.";

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
