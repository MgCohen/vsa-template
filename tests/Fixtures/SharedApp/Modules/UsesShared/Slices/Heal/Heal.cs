using Pipeline.Contracts.Commands;

namespace UsesShared.Slices.Heal;

// A command of the module, on the Shared base of its layer: Step -> ICommand.
public sealed record Heal(int Amount) : Step<HealResult>;

public sealed record HealResult(int Healed);
