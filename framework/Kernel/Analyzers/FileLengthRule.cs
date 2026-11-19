namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public sealed class FileLengthRule : LimitRule {
    public override int Number => 3;

    protected override int DefaultMax => 300;

    protected override string Title => "A file has at most the maximum number of lines";

    protected override string Message =>
        "The file has {0} lines, and the limit is {1}. Move a type or a part of a type to a new file.";

    protected internal override void Register(CompilationStartAnalysisContext start) =>
        start.RegisterSyntaxTreeAction(Check);

    void Check(SyntaxTreeAnalysisContext context) {
        if (Max(context.Options) is not { } max) {
            return;
        }
        var lines = context.Tree.GetText(context.CancellationToken).Lines;
        var count = lines.Count > 0 && lines[lines.Count - 1].Span.IsEmpty ? lines.Count - 1 : lines.Count;
        if (count <= max) {
            return;
        }
        context.ReportDiagnostic(Create(Location.Create(context.Tree, lines[max].Span), count, max));
    }
}
