namespace Umpire.CheckModel;

public sealed class StateFolder {
    public const string Key = "umpire.check.state";
    const string DefaultFolder = "artifacts~/umpire-model";
    const string Extension = ".json";

    StateFolder(string folder) {
        Folder = folder;
    }

    public string Folder { get; }
    public string DefinitionFile => Path.Combine(Folder, "definition" + Extension);
    public string BlockFile => Path.Combine(Folder, "block" + Extension);
    public bool HasDefinition => File.Exists(DefinitionFile) && File.Exists(BlockFile);
    public IReadOnlyList<string> SnapshotFiles => FilesIn(Snapshots);
    public IReadOnlyList<string> LibraryFiles => FilesIn(Libraries);

    string Snapshots => Path.Combine(Folder, "snapshots");
    string Libraries => Path.Combine(Folder, "libraries");

    public static StateFolder At(string folder) => new(BlockPath.Full(folder));

    public static StateFolder Of(string block, string? configured = null) =>
        new(BlockPath.In(BlockPath.Full(block), string.IsNullOrWhiteSpace(configured) ? DefaultFolder : configured!));

    public static StateFolder? Above(string? folder) {
        for (var current = folder; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current)) {
            if (Of(current!) is { HasDefinition: true } state) {
                return state;
            }
        }
        return null;
    }

    public string BlockOf(BlockSettings settings) => BlockPath.In(Folder, settings.Block);

    public string ReadDefinition() => StateFile.Read(DefinitionFile);

    public BlockSettings ReadBlock() => BlockSettings.Read(StateFile.Read(BlockFile));

    public string LibraryFile(string library) => Path.Combine(Libraries, library + Extension);

    public string SnapshotFile(string project) => Path.Combine(Snapshots, project + Extension);

    public void Save(Definition definition, BlockSettings settings) {
        Save(definition);
        Save(settings with { DefinitionHash = FileHash.OfText(definition.Write()) });
    }

    public bool Save(Definition definition) => StateFile.Save(DefinitionFile, definition.Write());

    public bool Save(BlockSettings settings) => StateFile.Save(BlockFile, settings.Write());

    public bool Save(Snapshot snapshot) => StateFile.Save(SnapshotFile(snapshot.Project), snapshot.Write());

    public bool SaveLibrary(Snapshot library) => StateFile.Save(LibraryFile(library.Project), library.Write());

    static IReadOnlyList<string> FilesIn(string folder) => Directory.Exists(folder)
        ? [.. Directory.GetFiles(folder, "*" + Extension).OrderBy(f => f, StringComparer.OrdinalIgnoreCase)]
        : [];
}
