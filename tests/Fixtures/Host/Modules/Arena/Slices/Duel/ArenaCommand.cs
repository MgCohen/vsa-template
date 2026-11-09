using VSA.Contracts.Commands;

namespace Arena.Slices.Duel;

public abstract record ArenaCommand<TResult> : ArenaMove, ICommand<TResult>;
