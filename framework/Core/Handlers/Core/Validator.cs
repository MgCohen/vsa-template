using VSA.Contracts;

namespace VSA.Handlers;

public abstract class Validator<TMessage> : IValidation where TMessage : class {
    public virtual int Order => 0;

    public abstract Error? Validate(IEnvelope<TMessage> envelope);

    Error? IValidation.Validate(Envelope envelope) => Validate((IEnvelope<TMessage>)envelope);
}
