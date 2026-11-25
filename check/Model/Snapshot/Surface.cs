namespace Umpire.CheckModel;

using System.Runtime.CompilerServices;

static class Surface {
    static readonly ConditionalWeakTable<Snapshot, string> Known = new();

    public static string Of(Snapshot snapshot) => Known.GetValue(snapshot, Make);

    static string Make(Snapshot snapshot) => FileHash.OfText(JsonWriter.Write(snapshot.Files
        .SelectMany(f => f.Types)
        .Select(Plain)
        .Select(JsonWriter.Write)
        .OrderBy(text => text, StringComparer.Ordinal)
        .ToList()));

    static TypeFacts Plain(TypeFacts type) => new(
        type.Name,
        type.Location with { Line = 0, Column = 0 },
        type.Content with { Members = [.. type.Content.Members.Select(m => m with { Line = 0 })] },
        []);
}
