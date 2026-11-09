namespace VSA.Handlers;

sealed record PipelineDefinition(
    Type Message,
    IReadOnlyList<Type> HandlerBases,
    IReadOnlyList<Type> Handlers,
    Type Runner,
    IReadOnlyList<Type> BeforeSteps,
    IReadOnlyList<Type> AfterSteps);
