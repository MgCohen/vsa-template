namespace Arena.Slices.Duel;

using Umpire.Vsa.Contracts.Commands;

public abstract record ArenaCommand<TResult> : ArenaMove, ICommand<TResult>;
