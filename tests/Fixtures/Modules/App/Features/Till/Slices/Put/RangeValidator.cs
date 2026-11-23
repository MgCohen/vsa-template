namespace Till.Slices.Put;

using Till.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

sealed class RangeValidator<T> : Validator<PutCommand<T>> where T : struct, IAmount {
    public override Error? Validate(IEnvelope<PutCommand<T>> envelope) =>
        envelope.Message.Amount < 0 ? TillErrors.Negative : null;
}
