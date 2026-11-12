using VSA.Contracts;
using VSA.Handlers;

namespace Arena.Slices.Duel;

sealed class ArmorRule : Before<StrikeCommand> {
    public override Task<StrikeCommand> BeforeAsync(IEnvelope<StrikeCommand> envelope) =>
        Task.FromResult(envelope.Message with { Damage = Math.Max(0, envelope.Message.Damage - 1) });
}
