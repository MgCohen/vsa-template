namespace Arena.Slices.Duel;

public sealed record ComboCommand(int Hits, int Damage) : ArenaCommand<int>;
