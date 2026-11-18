namespace Steps.Infrastructure;

using Steps.Domain.Shared;

public static class RunCodes {
    public static readonly string CouldNotRun = StepCode.Run(1);
    public static readonly string Skipped = StepCode.Run(2);
}
