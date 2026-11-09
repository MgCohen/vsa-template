using VSA.Contracts;
using VSA.Handlers;

namespace Arena.Slices.Duel;

sealed class StrikeValidator : Validator<StrikeCommand> {
    public override Error? Validate(IEnvelope<StrikeCommand> envelope) =>
        envelope.Message.Damage < 0 ? new Error("arena.negative", "A strike has no negative damage.") : null;
}
