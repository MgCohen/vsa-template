namespace Umpire.Messages.Handlers;

sealed record PipelineDefinition(
    Type Message,
    IReadOnlyList<Type> HandlerBases,
    IReadOnlyList<Type> Handlers,
    IReadOnlyList<Type> HookBases,
    Type Runner,
    IReadOnlyList<Type> BeforeSteps,
    IReadOnlyList<Type> AfterSteps) {
    internal IEnumerable<Type> SelectedBases => HandlerBases.Concat(Handlers);

    internal bool Selects(Type handlerBase) => SelectedBases.Any(t => MessageTypes.DerivesFrom(t, handlerBase));

    internal bool Overlaps(PipelineDefinition other) =>
        Message == other.Message
        && (SelectedBases.Any(t => other.SelectedBases.Any(o => Related(t, o)))
            || (!SelectedBases.Any() && !other.SelectedBases.Any()));

    static bool Related(Type one, Type other) =>
        MessageTypes.DerivesFrom(one, other) || MessageTypes.DerivesFrom(other, one);
}
