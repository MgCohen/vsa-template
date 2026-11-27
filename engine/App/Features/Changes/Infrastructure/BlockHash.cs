namespace Changes.Infrastructure;

using System.Text;

public static class BlockHash {
    const int ViewLength = 16;

    public static string View(string definition, IReadOnlyList<string> dlls, string run) =>
        Hash([definition, run, .. dlls.Select(dll => Path.GetFileName(dll)).Order(StringComparer.OrdinalIgnoreCase)])
            .Substring(0, ViewLength);

    public static string Inputs(string block, string definition, IReadOnlyList<string> dlls, string run) {
        var lines = new List<string> { definition, run, InputHash.Engine() };
        foreach (var dll in dlls.OrderBy(dll => Path.GetFileName(dll), StringComparer.OrdinalIgnoreCase)) {
            lines.Add($"{Path.GetFileName(dll)}|{Content(dll)}|{Content(Path.ChangeExtension(dll, ".pdb"))}");
        }
        lines.AddRange(InputHash.ConfigFacts(block));
        return Hash(lines);
    }

    static string Content(string file) {
        if (!File.Exists(file)) {
            return "";
        }
        using var stream = File.OpenRead(file);
        return PlatformPolyfills.Sha256Hex(stream);
    }

    static string Hash(IEnumerable<string> lines) =>
        PlatformPolyfills.Sha256Hex(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
}
