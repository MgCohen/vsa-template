namespace Build.Slices.BuildApp;

using Steps.Domain.Shared;

public static class BuildCodes {
    public static readonly string Failed = StepCode.Of<BuildAppCommand>(1);
    public static readonly string UnityCompiles = StepCode.Of<BuildAppCommand>(2);
}
