namespace Umpire.Cli.Scaffolding;

static class FileWriter {
    public static void WriteAllOrNone(string block, IEnumerable<TemplateFile> files) {
        var all = files.ToList();
        var existing = all.Where(f => File.Exists(FullPath(block, f))).Select(f => f.Path).ToList();
        if (existing.Count > 0) {
            var list = string.Join(Environment.NewLine + "  ", existing);
            throw new UmpireException(
                $"These files exist. The tool does not write over a file:{Environment.NewLine}  {list}");
        }
        foreach (var file in all) {
            var full = FullPath(block, file);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, file.Content);
            Console.WriteLine($"  created {file.Path}");
        }
    }

    static string FullPath(string block, TemplateFile file) => Path.GetFullPath(Path.Combine(block, file.Path));
}
