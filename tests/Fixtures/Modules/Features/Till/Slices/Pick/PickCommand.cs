namespace Till.Slices.Pick;

using Umpire.Vsa.Contracts.Commands;

public sealed record PickCommand<T> : ICommand<string> where T : struct;
