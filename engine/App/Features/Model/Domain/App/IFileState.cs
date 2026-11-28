namespace Umpire.CheckModel;

public interface IFileState {
    bool IsCurrent(FileMark mark, string block);

    bool IsCurrent(string file, string hash, long modified);

    bool HasNewSources(AppModel app);
}
