namespace Versions.Slices.CheckVersions;

using Steps.Contracts.Commands;
using Steps.Domain.Shared;
using Versions.Domain.Shared;

public sealed record CheckVersionsCommand(string BlockFolder, string Bin) : EngineStep<CheckVersionsResult>, IStepGroup {
    public static int Group => 2;
}
