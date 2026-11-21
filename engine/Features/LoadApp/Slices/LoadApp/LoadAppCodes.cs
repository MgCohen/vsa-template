namespace LoadApp.Slices.LoadApp;

using Steps.Domain.Shared;

public static class LoadAppCodes {
    public static readonly string NoAppAssembly = StepCode.Of<LoadAppCommand>(1);
    public static readonly string NoAppClass = StepCode.Of<LoadAppCommand>(2);
    public static readonly string ManyAppClasses = StepCode.Of<LoadAppCommand>(3);
    public static readonly string NoPackedConstructor = StepCode.Of<LoadAppCommand>(5);
    public static readonly string ManyPacks = StepCode.Of<LoadAppCommand>(6);
}
