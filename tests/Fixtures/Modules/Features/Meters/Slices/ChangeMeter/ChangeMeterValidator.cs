namespace Meters.Slices.ChangeMeter;

using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

sealed class ChangeMeterValidator<T> : Validator<ChangeMeterCommand<T>> where T : struct {
    public override Error? Validate(IEnvelope<ChangeMeterCommand<T>> envelope) =>
        envelope.Message.Delta == 0 ? MeterErrors.NoChange : null;
}
