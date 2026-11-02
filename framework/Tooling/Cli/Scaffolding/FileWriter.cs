namespace VSA.Cli.Scaffolding;

static class FileWriter {
    // Writes all files, or none. The tool does not write over a file that exists.
    public static void Write(string root, IEnumerable<(string Path, string Content)> files) {
        var all = files.Select(f => (f.Path, Full: Path.GetFullPath(Path.Combine(root, f.Path)), f.Content)).ToList();
        var existing = all.Where(f => File.Exists(f.Full)).Select(f => f.Path).ToList();
        if (existing.Count > 0) {
            throw new VsaException($"These files exist. The tool does not write over a file:{Environment.NewLine}  {string.Join(Environment.NewLine + "  ", existing)}");
        }
        foreach (var file in all) {
            Directory.CreateDirectory(Path.GetDirectoryName(file.Full)!);
            File.WriteAllText(file.Full, file.Content);
            Console.WriteLine($"  created {file.Path}");
        }
    }
}
