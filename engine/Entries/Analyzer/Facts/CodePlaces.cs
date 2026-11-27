namespace UmpireEngine;

using Facts.Domain.Shared.Compile;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

static class CodePlaces {
    public static CodePlace Of(Location location) => location.SourceTree is { } tree
        ? Of(tree.GetText(), tree.FilePath, location.SourceSpan)
        : new CodePlace("", 1, 1, 0);

    public static CodePlace Of(SourceText text, string file, TextSpan span) {
        var start = text.Lines.GetLinePosition(span.Start);
        return new CodePlace(file, start.Line + 1, start.Character + 1, span.Length);
    }
}
