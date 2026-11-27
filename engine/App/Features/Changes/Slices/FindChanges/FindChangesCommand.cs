namespace Changes.Slices.FindChanges;

using Changes.Domain.Shared;
using Operations.Contracts.Commands;
using Operations.Domain.Shared;

public sealed record FindChangesCommand(string Block, string Bin, string Run)
    : JobOperation<ChangesResult>, IOperationGroup {
    public static int Group => 2;
}
