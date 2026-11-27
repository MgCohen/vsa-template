namespace Operations.Infrastructure;

using Operations.Domain.Shared;

public static class RunCodes {
    public static readonly string CouldNotRun = OperationCode.Run(1);
    public static readonly string Skipped = OperationCode.Run(2);
    public static readonly string NoEngine = OperationCode.Run(3);
    public static readonly string EngineCrash = OperationCode.Run(4);
}
