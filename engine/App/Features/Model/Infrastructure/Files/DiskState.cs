namespace Umpire.CheckModel;

using Facts.Infrastructure;

sealed class DiskState : IFileState {
    public static DiskState Instance { get; } = new();

    public bool IsCurrent(FileMark mark, string block) => DiskFiles.IsCurrent(mark, block);

    public bool IsCurrent(string file, string hash, long modified) => DiskFiles.IsCurrent(file, hash, modified);

    public bool HasNewSources(AppModel app) => SourceFiles.HasNew(app);
}
