namespace Blocks.Slices.FindBlocks;

using BuiltApp.Domain.Shared;

static class UsedBlocks {
    public static IReadOnlyList<string> Of(
        string folder, IReadOnlyList<BlockEntry> inner, IReadOnlyList<BlockEntry> entries, BinIndex bin) {
        var own = bin.AppDlls(folder, []);
        var referenced = bin.ReferencedDlls(own, [], shared: true).Where(dll => dll.IsProject).ToList();
        var blocks = entries.Select(e => e.Folder).Where(f => !IsAtOrUnder(folder, f)).ToList();
        var used = referenced.Select(dll => Deepest(dll, blocks)).OfType<string>();
        return [.. inner.Select(e => e.Folder).Concat(used)
            .Where(f => !Same(f, folder) && Directory.Exists(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)];
    }

    static string? Deepest(BinDll dll, IReadOnlyList<string> blocks) =>
        blocks.Where(block => dll.HasSourceUnder(block)).OrderByDescending(f => f.Length).FirstOrDefault();

    static bool IsAtOrUnder(string path, string folder) =>
        Same(path, folder) || path.StartsWith(folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    static bool Same(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
