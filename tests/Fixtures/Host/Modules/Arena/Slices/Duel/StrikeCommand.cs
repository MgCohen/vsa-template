namespace Arena.Slices.Duel;

public sealed record StrikeCommand(int Damage) : ArenaCommand<int>;
