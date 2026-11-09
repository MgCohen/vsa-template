using Pipeline.Contracts.Commands;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Pipeline.Handlers.Commands.Steps;

// The base of the step handlers of each module, as Handler<TCommand, TResult, TScope> in a game engine.
// The router finds CommandHandler through this base, in the Handlers/Commands folder of Pipeline.
public abstract class StepHandler<TStep, TResult> : CommandHandler<TStep, TResult> where TStep : Step<TResult> {
    protected abstract TResult Run(TStep step);

    public sealed override Task<Result<TResult>> HandleAsync(Envelope<TStep> envelope) =>
        Task.FromResult<Result<TResult>>(Run(envelope.Message));
}
