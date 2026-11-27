namespace Umpire.Engine.Polyfills;

using System.Runtime.InteropServices;
using System.Security;

sealed class FolderSearch {
    static readonly StringComparison NameComparison =
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    public FileAttributes AttributesToSkip { get; init; } = FileAttributes.Hidden | FileAttributes.System;

    public bool RecurseSubdirectories { get; init; }

    public IEnumerable<string> Files(string folder, string pattern) =>
        Entries(folder).Where(entry => entry is FileInfo && Matches(entry.Name, pattern)).Select(entry => entry.FullName);

    public IEnumerable<string> Folders(string folder, string pattern) =>
        Entries(folder).Where(entry => entry is DirectoryInfo && Matches(entry.Name, pattern)).Select(entry => entry.FullName);

    static FileSystemInfo[] Read(DirectoryInfo folder) {
        try {
            return folder.GetFileSystemInfos();
        } catch (Exception e) when (e is UnauthorizedAccessException or SecurityException) {
            return [];
        }
    }

    static bool Matches(string name, string pattern) => Matches(name, 0, pattern, 0);

    static bool Matches(string name, int at, string pattern, int from) {
        if (from == pattern.Length) {
            return at == name.Length;
        }
        if (pattern[from] == '*') {
            for (var next = at; next <= name.Length; next++) {
                if (Matches(name, next, pattern, from + 1)) {
                    return true;
                }
            }
            return false;
        }
        return at < name.Length
            && (pattern[from] == '?' || string.Equals(name[at].ToString(), pattern[from].ToString(), NameComparison))
            && Matches(name, at + 1, pattern, from + 1);
    }

    IEnumerable<FileSystemInfo> Entries(string folder) {
        var pending = new Queue<DirectoryInfo>();
        var top = new DirectoryInfo(folder);
        if (!top.Exists) {
            throw new DirectoryNotFoundException($"Could not find a part of the path '{top.FullName}'.");
        }
        pending.Enqueue(top);
        while (pending.Count > 0) {
            foreach (var entry in Read(pending.Dequeue())) {
                if ((entry.Attributes & AttributesToSkip) != 0) {
                    continue;
                }
                if (RecurseSubdirectories && entry is DirectoryInfo directory) {
                    pending.Enqueue(directory);
                }
                yield return entry;
            }
        }
    }
}
