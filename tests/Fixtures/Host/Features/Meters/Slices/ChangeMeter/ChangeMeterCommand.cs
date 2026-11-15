namespace Meters.Slices.ChangeMeter;

using Umpire.Vsa.Contracts.Commands;

public sealed record ChangeMeterCommand<T>(int Delta) : ICommand<string> where T : struct;
