using Umpire.Vsa.Contracts.Commands;

namespace Arena.Slices.Duel;

public abstract record ArenaCommand<TResult> : ArenaMove, ICommand<TResult>;
