namespace Unbound.Slices.Mark;

using Umpire.Vsa.Contracts.Commands;

public sealed record MarkCommand<T> : ICommand<int>;
