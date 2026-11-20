namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;

[AutoRegister]
public abstract class AfterCommandHook<TCommand, TResult> : IAfterCall where TCommand : class, ICommand<TResult> {
    public abstract Task AfterAsync(IEnvelope<TCommand> envelope, Result<TResult> outcome);

    Task IAfterCall.CallAsync(Envelope envelope) =>
        AfterAsync((IEnvelope<TCommand>)envelope, EnvelopeResults.Of<TResult>(envelope));
}
