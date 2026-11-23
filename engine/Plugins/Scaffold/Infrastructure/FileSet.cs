namespace Scaffold.Infrastructure;

using Scaffold.Domain.Shared;
public static class FileSet {
    public static IReadOnlyList<string> Existing(string block, IEnumerable<PlannedFile> files) =>
        [.. files.Select(file => file.Path).Where(path => File.Exists(FullPath(block, path)))];

    public static void Write(string block, IEnumerable<PlannedFile> files) {
        foreach (var file in files) {
            var full = FullPath(block, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, file.Content);
        }
    }

    static string FullPath(string block, string path) => Path.GetFullPath(Path.Combine(block, path));
}
