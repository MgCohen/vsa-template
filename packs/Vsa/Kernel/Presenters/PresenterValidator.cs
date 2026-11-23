namespace Umpire.Vsa.Presenters;

using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

[AutoRegister]
public abstract class PresenterValidator<TEvent> : IValidation where TEvent : class {
    public abstract Error? Validate(IEnvelope<TEvent> envelope);

    Error? IValidation.Validate(Envelope envelope) => Validate((IEnvelope<TEvent>)envelope);
}
