namespace Umpire.Vsa.Handlers.Commands;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class BeforeCommandHook<TCommand> : IBeforeCall where TCommand : class, ICommand {
    public abstract Task<TCommand> BeforeAsync(IEnvelope<TCommand> envelope);

    async Task<object> IBeforeCall.CallAsync(Envelope envelope) => await BeforeAsync((IEnvelope<TCommand>)envelope);
}
