using VSA.Analyzers.Markers;
using VSA.Contracts;
using VSA.Contracts.Commands;

namespace VSA.Handlers.Commands;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class BeforeCommandHook<TCommand> : IBeforeCall where TCommand : class, ICommand {
    public abstract Task<TCommand> BeforeAsync(IEnvelope<TCommand> envelope);

    async Task<object> IBeforeCall.CallAsync(Envelope envelope) => await BeforeAsync((IEnvelope<TCommand>)envelope);
}
