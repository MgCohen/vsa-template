namespace Umpire.CheckModel;

public static class AppModels {
    public static AppModel Load(StateFolder state, CurrentProject? current = null, CheckMode? mode = null) =>
        Load(state, state.ReadBlock(), current, mode);

    public static AppModel Load(
        StateFolder state, BlockSettings settings, CurrentProject? current = null, CheckMode? mode = null, string? target = null) {
        var text = state.ReadDefinition();
        return Of(Definition.Read(text), settings with { Mode = mode ?? settings.Mode }, state, current,
            FileHash.OfText(text) == settings.DefinitionHash, target);
    }

    public static AppModel Load(Definition definition, StateFolder state, CurrentProject? current = null) =>
        Of(definition, state.ReadBlock(), state, current, true, null);

    static AppModel Of(
        Definition definition, BlockSettings settings, StateFolder state, CurrentProject? current, bool isDefinitionOfBlock, string? target) =>
        new(definition, settings, state.BlockOf(settings), SnapshotLoad.Of(state, settings, current?.Snapshot, target), current,
            isDefinitionOfBlock, DiskState.Instance);
}
