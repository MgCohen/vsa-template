using Steps.Domain.Shared;
using VSA.Contracts.Commands;

namespace Steps.Contracts.Commands;

public abstract record EngineStep<TResult> : ICommand<TResult>, IEngineStep;
