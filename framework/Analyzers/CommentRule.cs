using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1013: the code has no comments: no //, no /* */ and no ///. The names of the code give the information.
// A directive, for example #region or #pragma, is not a comment. VsaAnalyzer does not check generated code.
public sealed class CommentRule() : VsaAnalyzerRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1013",
        "The code has no comments",
        "The code has {0}. Remove it. Put the information in a name: a type, a member, a variable or a test.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public override bool IsModuleRule => false;

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        foreach (var trivia in context.Tree.GetRoot(context.CancellationToken).DescendantTrivia()) {
            if (Kind(trivia) is { } kind) {
                context.ReportDiagnostic(Create(Location.Create(context.Tree, trivia.FullSpan), kind));
            }
        }
    }

    // The span of an XML comment starts after ///, so the location uses the full span.
    // With DocumentationMode None, the parser reads /// as a line comment, so the text also selects the kind.
    static string? Kind(SyntaxTrivia trivia) => trivia.Kind() switch {
        SyntaxKind.SingleLineDocumentationCommentTrivia or SyntaxKind.MultiLineDocumentationCommentTrivia => "an XML comment",
        SyntaxKind.SingleLineCommentTrivia when trivia.ToString().StartsWith("///") => "an XML comment",
        SyntaxKind.SingleLineCommentTrivia => "a line comment",
        SyntaxKind.MultiLineCommentTrivia => "a block comment",
        _ => null,
    };
}
