using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

namespace Steps.Contracts.Commands;

public abstract record EngineStep<TResult> : ICommand<TResult>, IEngineStep;
