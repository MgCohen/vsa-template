using Umpire.Markers;
using Umpire.Messages.Contracts;

namespace Umpire.Messages.Handlers;

[AutoRegister]
[RequireDerived]
[Preserve]
public abstract class Validator<TMessage> : IValidation where TMessage : class {
    public abstract Error? Validate(IEnvelope<TMessage> envelope);

    Error? IValidation.Validate(Envelope envelope) => Validate((IEnvelope<TMessage>)envelope);
}
