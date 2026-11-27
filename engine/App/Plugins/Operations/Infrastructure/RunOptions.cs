namespace Operations.Infrastructure;

using System.Text;
using Operations.Domain.Shared;

public sealed class RunOptions {
    public const string SnapshotList = "snapshots.txt";
    const string UsedFolder = "used";
    const string BinFolder = "bin";
    const string ObjFolder = "obj";
    const string ArtifactsFolder = "artifacts~";
    const string UmpireFolder = "umpire";
    const string CheckFolder = "check";
    const int KeyLength = 8;

    public bool FailFast { get; set; }

    public IReadOnlyList<string> References { get; set; } = [];

    public string? SourceRoot { get; set; }

    public IReadOnlyList<string> IgnoredFolders { get; set; } = [];

    public bool ProjectChecks { get; set; } = true;

    public string? State { get; set; }

    public string Snapshots { get; set; } = "";

    public string? ConfiguredState { get; set; }

    public void Apply(CheckSettings settings) {
        References = settings.References;
        SourceRoot = settings.SourceRoot;
        IgnoredFolders = settings.IgnoredFolders;
        ProjectChecks = settings.ProjectChecks;
        ConfiguredState = settings.State;
    }

    public void Use(string state, string snapshots, string bin) {
        var own = OwnFolderOf(bin);
        State = state.Length > 0 ? Path.GetFullPath(state) : ConfiguredState ?? (own is null ? null : Path.Combine(own, CheckFolder));
        Snapshots = snapshots.Length > 0 ? Path.GetFullPath(snapshots)
            : own is not null && File.Exists(Path.Combine(own, SnapshotList)) ? Path.Combine(own, SnapshotList)
            : State is { } folder ? Path.Combine(folder, SnapshotList)
            : "";
    }

    public static string? OwnFolderOf(string bin) {
        if (bin.Length == 0) {
            return null;
        }
        var parts = Path.GetFullPath(bin).TrimEnd('/', '\\').Split('/', '\\');
        var last = Array.FindLastIndex(parts, part => string.Equals(part, BinFolder, StringComparison.OrdinalIgnoreCase));
        if (last < 0) {
            return null;
        }
        parts[last] = ObjFolder;
        return Path.Combine(string.Join(Path.DirectorySeparatorChar.ToString(), parts), UmpireFolder);
    }

    public string StateOf(string block, bool isUsed) {
        if (State is not { } folder) {
            return Path.Combine(Path.GetFullPath(block), ArtifactsFolder, UmpireFolder);
        }
        if (!isUsed) {
            return folder;
        }
        var full = Path.GetFullPath(block);
        var key = PlatformPolyfills.Sha256Hex(Encoding.UTF8.GetBytes(full.ToLowerInvariant())).Substring(0, KeyLength);
        return Path.Combine(folder, UsedFolder, $"{Path.GetFileName(full.TrimEnd('/', '\\'))}-{key}");
    }

    public string StampFolder(string bin) => State ?? bin;

    public string Inputs(ReportMode? mode, string skip, string block) =>
        $"{mode}|{skip}|{block}|{string.Join(";", References)}|{SourceRoot}"
        + $"|{string.Join(";", IgnoredFolders)}|{ProjectChecks}";
}
