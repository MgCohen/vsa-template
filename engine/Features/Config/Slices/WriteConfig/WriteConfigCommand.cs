namespace Config.Slices.WriteConfig;

using Config.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

public sealed record WriteConfigCommand(string Folder) : EngineStep<WriteConfigResult>, IStepGroup {
    public static int Group => 1;
}
