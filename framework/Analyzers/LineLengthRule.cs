using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace VSA.Analyzers;

// VSA1008: a line has at most 120 characters. The diagnostic marks the characters after the limit.
// Option: vsa.VSA1008.max = 120.
public sealed class LineLengthRule() : LimitRule(Descriptor, 120) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1008",
        "A line has at most the maximum number of characters",
        "The line has {0} characters, and the limit is {1}. Break the line.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        var max = Max(context.Options, context.Tree);
        foreach (var line in context.Tree.GetText(context.CancellationToken).Lines) {
            if (line.Span.Length <= max) {
                continue;
            }
            var excess = TextSpan.FromBounds(line.Start + max, line.End);
            context.ReportDiagnostic(Create(Location.Create(context.Tree, excess), line.Span.Length, max));
        }
    }
}
