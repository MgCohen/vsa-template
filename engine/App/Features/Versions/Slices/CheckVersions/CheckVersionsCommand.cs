namespace Versions.Slices.CheckVersions;

using Operations.Contracts.Commands;
using Operations.Domain.Shared;
using Versions.Domain.Shared;

public sealed record CheckVersionsCommand(string BlockFolder, string Bin)
    : JobOperation<CheckVersionsResult>, IOperationGroup {
    public static int Group => 4;
}
