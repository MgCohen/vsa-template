namespace CheckModel.Tests;

using Umpire.CheckModel;

sealed class TempState : IDisposable {
    public TempState() {
        Block = Directory.CreateTempSubdirectory("check").FullName;
        State = StateFolder.Of(Block);
    }

    public string Block { get; }
    public StateFolder State { get; }

    public AppModel Load(Definition definition, params FileFacts[] files) {
        State.Save(BlockSettings.Of("../..", "Sample"));
        State.Save(Snapshot.Of("Sample", files));
        return AppModel.Load(definition, State);
    }

    public void Dispose() => Directory.Delete(Block, recursive: true);
}
