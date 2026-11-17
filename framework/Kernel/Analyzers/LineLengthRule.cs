namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

public sealed class LineLengthRule : LimitRule {
    public override int Number => 1;

    protected override int DefaultMax => 120;

    protected override string Title => "A line has at most the maximum number of characters";

    protected override string Message =>
        "The line has {0} characters, and the limit is {1}. Break the line.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        if (Max(context.Options, context.Tree) is not { } max) {
            return;
        }
        foreach (var line in context.Tree.GetText(context.CancellationToken).Lines) {
            if (line.Span.Length <= max) {
                continue;
            }
            var excess = TextSpan.FromBounds(line.Start + max, line.End);
            context.ReportDiagnostic(Create(Location.Create(context.Tree, excess), line.Span.Length, max));
        }
    }
}
