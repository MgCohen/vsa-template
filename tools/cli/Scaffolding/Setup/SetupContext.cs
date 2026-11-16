namespace Umpire.Cli.Scaffolding.Setup;

sealed class SetupContext(
    string block,
    string rootProject,
    string version,
    IReadOnlyList<string> sources,
    bool keepsParentFiles) {
    readonly Dictionary<string, string> planned = new(StringComparer.Ordinal);

    public string Block => block;

    public string RootProject => rootProject;

    public string Version => version;

    public IReadOnlyList<string> Sources => sources;

    public IReadOnlyDictionary<string, string> Planned => planned;

    public string? Read(string path) {
        if (planned.TryGetValue(path, out var content)) {
            return content;
        }
        var full = FullPath(path);
        return File.Exists(full) ? File.ReadAllText(full).Replace("\r\n", "\n") : null;
    }

    public bool OnDisk(string path) => File.Exists(FullPath(path));

    public string? ParentFile(string path) {
        if (!keepsParentFiles) {
            return null;
        }
        for (var folder = Directory.GetParent(Path.GetFullPath(block)); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, path);
            if (File.Exists(file)) {
                return file;
            }
        }
        return null;
    }

    public void Add(TemplateFile file) => planned[file.Path] = file.Content;

    public string FullPath(string path) => Path.GetFullPath(Path.Combine(block, path));
}
