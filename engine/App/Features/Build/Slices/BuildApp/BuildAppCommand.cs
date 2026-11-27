namespace Build.Slices.BuildApp;

using Build.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record BuildAppCommand(string Project, string Bin)
    : JobOperation<BuildAppResult>, IOperationGroup {
    public static int Group => 1;
}
