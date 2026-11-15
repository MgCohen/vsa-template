namespace Arena.Slices.Armor;

using Arena.Slices.Duel;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

sealed class ArmorHook : BeforeCommandHook<StrikeCommand> {
    public override Task<StrikeCommand> BeforeAsync(IEnvelope<StrikeCommand> envelope) =>
        Task.FromResult(envelope.Message with { Damage = Math.Max(0, envelope.Message.Damage - 1) });
}
