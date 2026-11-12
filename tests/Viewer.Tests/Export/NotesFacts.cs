using System.Text.Json;

namespace Viewer.Tests.Export;

public static class NotesFacts
{
    private static readonly Lazy<JsonElement> root = new(() => JsonDocument.Parse(NotesExport.Json).RootElement);

    public static int PartCards => root.Value.GetProperty("definition").GetProperty("parts").GetArrayLength() - 1;

    public static int NodeEdges => root.Value.GetProperty("app").GetProperty("dependencies").EnumerateArray()
        .Select(d => (From: d.GetProperty("from").GetString(), To: d.GetProperty("to").GetString()))
        .Where(d => d.From != d.To)
        .Distinct()
        .Count();

    public static IReadOnlySet<string> RuleTerms => root.Value.GetProperty("definition").GetProperty("rules").EnumerateArray()
        .SelectMany(r => r.GetProperty("from").EnumerateArray().SelectMany(f => r.GetProperty("to").EnumerateArray()
            .Select(t => (Rule: r.GetProperty("id").GetString(), From: Layer(f), To: Layer(t)))))
        .Where(t => t.From != t.To)
        .Select(t => $"{t.Rule}:{t.From}>{t.To}")
        .ToHashSet();

    private static string? Layer(JsonElement term)
    {
        return term.GetProperty("layer").GetString();
    }
}
