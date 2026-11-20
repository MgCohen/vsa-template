namespace BuiltApp.Domain.Shared;

using Umpire.Architecture;

public sealed record BinIndex(string Bin, IReadOnlyList<BinDll> Dlls, IReadOnlyList<BlockEntry> Entries) {
    public static IReadOnlyDictionary<string, Source> Sources(IEnumerable<BinDll> dlls) {
        var sources = new Dictionary<string, Source>();
        foreach (var type in dlls.SelectMany(d => d.Types)) {
            sources.TryAdd(type.FullName, type.Source);
        }
        return sources;
    }

    public IReadOnlyList<BinDll> AppDlls(string blockFolder, IReadOnlyList<string> skip) =>
        [.. Dlls.Where(d => !Skipped(d.Name, skip) && d.HasSourceUnder(blockFolder))];

    public IReadOnlyList<BinDll> ExternalDlls(IReadOnlyList<string> names) =>
        [.. Dlls.Where(d => names.Any(e => d.Name == e || d.Name.StartsWith(e + ".", StringComparison.Ordinal)))];

    public IReadOnlyList<BinDll> ReferencedDlls(
        IReadOnlyList<BinDll> app, IReadOnlyList<string> skip, bool shared = false) {
        var byName = Dlls.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);
        var found = new Dictionary<string, BinDll>(StringComparer.OrdinalIgnoreCase);
        var next = new Queue<BinDll>(app);
        while (next.TryDequeue(out var dll)) {
            foreach (var reference in dll.References) {
                if (BinDll.IsSystem(reference) || found.ContainsKey(reference) || Skipped(reference, skip)
                    || app.Any(a => a.Name == reference) || !byName.TryGetValue(reference, out var referenced)
                    || (referenced.IsShared && !shared)) {
                    continue;
                }
                found[reference] = referenced;
                next.Enqueue(referenced);
            }
        }
        return [.. found.Values];
    }

    static bool Skipped(string name, IReadOnlyList<string> skip) => skip.Contains(name, StringComparer.OrdinalIgnoreCase);
}
