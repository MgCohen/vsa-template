namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

using Umpire.Files;

sealed class SetupContext(
    string block,
    string rootProject,
    string version,
    IReadOnlyList<string> sources,
    bool existingApp) {
    readonly Dictionary<string, TextFile> planned = new(StringComparer.Ordinal);

    public string Block => block;

    public string RootProject => rootProject;

    public string Version => version;

    public IReadOnlyList<string> Sources => sources;

    public bool ExistingApp => existingApp;

    public IReadOnlyCollection<TextFile> Planned => planned.Values;

    public string? Read(string path) {
        if (planned.TryGetValue(path, out var file)) {
            return file.Content;
        }
        var full = FullPath(path);
        return existingApp && File.Exists(full) ? File.ReadAllText(full).Replace("\r\n", "\n") : null;
    }

    public bool OnDisk(string path) => File.Exists(FullPath(path));

    public string? ParentFile(string path) {
        if (!existingApp) {
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

    public void Add(TextFile file) => planned[file.Path] = file;

    string FullPath(string path) => Path.GetFullPath(Path.Combine(block, path));
}
