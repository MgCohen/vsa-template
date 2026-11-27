namespace Build.Slices.BuildApp;

using Operations.Domain.Shared;

public static class BuildCodes {
    public static readonly string Failed = OperationCode.Of<BuildAppCommand>(1);
}
