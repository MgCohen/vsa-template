using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// VSA1010: a file has at most max lines. The diagnostic marks the first line after the limit.
// Option: vsa.VSA1010.max, in VSA.Rules.globalconfig.
public sealed class FileLengthRule() : LimitRule(Descriptor) {
    public static readonly DiagnosticDescriptor Descriptor = new(
        "VSA1010",
        "A file has at most the maximum number of lines",
        "The file has {0} lines, and the limit is {1}. Move a type or a part of a type to a new file.",
        "VSA",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        if (Max(context.Options, context.Tree) is not { } max) {
            return;
        }
        var lines = context.Tree.GetText(context.CancellationToken).Lines;
        // The empty line after the last line break is not a line of the file.
        var count = lines.Count > 0 && lines[lines.Count - 1].Span.IsEmpty ? lines.Count - 1 : lines.Count;
        if (count <= max) {
            return;
        }
        context.ReportDiagnostic(Create(Location.Create(context.Tree, lines[max].Span), count, max));
    }
}
