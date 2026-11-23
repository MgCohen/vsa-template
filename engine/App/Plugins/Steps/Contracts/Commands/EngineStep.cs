namespace Steps.Contracts.Commands;

using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public abstract record EngineStep<TResult> : ICommand<TResult>, IEngineStep;
