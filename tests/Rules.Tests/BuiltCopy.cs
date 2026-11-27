using Tests.Shared;

namespace Rules.Tests;

sealed class BuiltCopy {
    static readonly string[] buildOutputs = ["bin", "umpire-checks"];
    readonly Workspace workspace;
    readonly string folder;
    readonly HashSet<string> rootEntries;

    BuiltCopy(Workspace workspace) {
        this.workspace = workspace;
        folder = Path.Combine(workspace.Root, "template");
        rootEntries = [.. Directory.EnumerateFileSystemEntries(workspace.Root), workspace.Src, folder];
    }

    public static BuiltCopy Of(Workspace workspace) {
        var copy = new BuiltCopy(workspace);
        Copy(workspace.Src, copy.folder);
        return copy;
    }

    public void Restore() {
        foreach (var entry in Directory.EnumerateFileSystemEntries(workspace.Root).Where(entry => !rootEntries.Contains(entry))) {
            if (Directory.Exists(entry)) {
                Directory.Delete(entry, recursive: true);
            } else {
                File.Delete(entry);
            }
        }
        Directory.Delete(workspace.Src, recursive: true);
        Copy(folder, workspace.Src);
    }

    static void Copy(string from, string to) {
        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from)) {
            var target = Path.Combine(to, Path.GetFileName(file));
            File.Copy(file, target);
            File.SetLastWriteTimeUtc(target, File.GetLastWriteTimeUtc(file));
        }
        foreach (var dir in Directory.EnumerateDirectories(from)) {
            var name = Path.GetFileName(dir);
            if (Path.GetFileName(from) != "artifacts~" || !buildOutputs.Contains(name)) {
                Copy(dir, Path.Combine(to, name));
            }
        }
    }
}
