namespace VSA.Cli.Scaffolding;

static class FileWriter {
    public static void WriteAllOrNone(string root, IEnumerable<TemplateFile> files) {
        var all = files.ToList();
        var existing = all.Where(f => File.Exists(FullPath(root, f))).Select(f => f.Path).ToList();
        if (existing.Count > 0) {
            var list = string.Join(Environment.NewLine + "  ", existing);
            throw new VsaException(
                $"These files exist. The tool does not write over a file:{Environment.NewLine}  {list}");
        }
        foreach (var file in all) {
            var full = FullPath(root, file);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, file.Content);
            Console.WriteLine($"  created {file.Path}");
        }
    }

    static string FullPath(string root, TemplateFile file) => Path.GetFullPath(Path.Combine(root, file.Path));
}
