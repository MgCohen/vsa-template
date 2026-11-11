using VSA.Contracts.Commands;

namespace Meters.Slices.ChangeMeter;

public sealed record ChangeMeterCommand<T>(int Delta) : ICommand<string> where T : struct;
