namespace LoadApp.Slices.LoadApp;

using Steps.Domain.Shared;

public static class LoadAppCodes {
    public static readonly string NoAppAssembly = StepCode.Of<LoadAppCommand>(1);
    public static readonly string NoAppClass = StepCode.Of<LoadAppCommand>(2);
    public static readonly string ManyAppClasses = StepCode.Of<LoadAppCommand>(3);
    public static readonly string NoPackedConstructor = StepCode.Of<LoadAppCommand>(5);
    public static readonly string ManyKernels = StepCode.Of<LoadAppCommand>(6);
    public static readonly string OtherKernel = StepCode.Of<LoadAppCommand>(7);
    public static readonly string ManyDefinitions = StepCode.Of<LoadAppCommand>(8);
    public static readonly string NoDefinition = StepCode.Of<LoadAppCommand>(9);
    public static readonly string MissingAssembly = StepCode.Of<LoadAppCommand>(10);
    public static readonly string IgnoredDefinition = StepCode.Of<LoadAppCommand>(11);
    public static readonly string EmptyDefinitionFolder = StepCode.Of<LoadAppCommand>(12);
}
