namespace Umpire.Messages.Handlers;

using Umpire.Kernels;

sealed record PipelineDefinition(
    Type Message,
    IReadOnlyList<Type> HandlerBases,
    IReadOnlyList<Type> Handlers,
    IReadOnlyList<Type> HookBases,
    IReadOnlyList<Type> ValidatorBases,
    Type Runner,
    IReadOnlyList<PipelineStep> BeforeSteps,
    IReadOnlyList<PipelineStep> AfterSteps,
    IReadOnlyList<Type> Calls,
    IReadOnlyList<AfterHandledCall> AfterHandled) : IDispatchClose {
    public IReadOnlyList<Type> ClosedServices => [typeof(IHandler<>), .. ValidatorBases, .. HookBases];

    internal IEnumerable<Type> SelectedBases => HandlerBases.Concat(Handlers);

    internal bool Selects(Type handlerBase) => SelectedBases.Any(t => MessageTypes.DerivesFrom(t, handlerBase));

    internal bool Overlaps(PipelineDefinition other) => Message == other.Message && SharesHandlers(other);

    internal bool SharesHandlers(PipelineDefinition other) =>
        SelectedBases.Any(t => other.SelectedBases.Any(o => Related(t, o)))
        || (!SelectedBases.Any() && !other.SelectedBases.Any());

    static bool Related(Type one, Type other) =>
        MessageTypes.DerivesFrom(one, other) || MessageTypes.DerivesFrom(other, one);
}
