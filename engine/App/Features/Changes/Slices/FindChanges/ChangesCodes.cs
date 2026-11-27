namespace Changes.Slices.FindChanges;

using Operations.Domain.Shared;

public static class ChangesCodes {
    public static readonly string Same = OperationCode.Of<FindChangesCommand>(1);
    public static readonly string BlockSame = OperationCode.Of<FindChangesCommand>(2);
}
