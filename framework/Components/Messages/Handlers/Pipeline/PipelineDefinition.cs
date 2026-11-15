namespace Umpire.Messages.Handlers;

sealed record PipelineDefinition(
    Type Message,
    IReadOnlyList<Type> HandlerBases,
    IReadOnlyList<Type> Handlers,
    IReadOnlyList<Type> HookBases,
    Type Runner,
    IReadOnlyList<Type> BeforeSteps,
    IReadOnlyList<Type> AfterSteps);
