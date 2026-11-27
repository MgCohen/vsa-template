namespace LoadApp.Slices.LoadApp;

using Operations.Domain.Shared;

public static class LoadAppCodes {
    public static readonly string NoAppAssembly = OperationCode.Of<LoadAppCommand>(1);
    public static readonly string NoAppClass = OperationCode.Of<LoadAppCommand>(2);
    public static readonly string ManyAppClasses = OperationCode.Of<LoadAppCommand>(3);
    public static readonly string NoPackedConstructor = OperationCode.Of<LoadAppCommand>(5);
    public static readonly string ManyKernels = OperationCode.Of<LoadAppCommand>(6);
    public static readonly string OtherKernel = OperationCode.Of<LoadAppCommand>(7);
    public static readonly string ManyDefinitions = OperationCode.Of<LoadAppCommand>(8);
    public static readonly string NoDefinition = OperationCode.Of<LoadAppCommand>(9);
    public static readonly string MissingAssembly = OperationCode.Of<LoadAppCommand>(10);
    public static readonly string IgnoredDefinition = OperationCode.Of<LoadAppCommand>(11);
    public static readonly string EmptyDefinitionFolder = OperationCode.Of<LoadAppCommand>(12);
    public static readonly string UnloadedDefinition = OperationCode.Of<LoadAppCommand>(13);
}
