using VSA.Contracts.Commands;

namespace UsesPlugins.Slices.ReadMeter;

public sealed record ReadMeterCommand : ICommand<int>;
