namespace LoadTemplates.Slices.LoadTemplates;

using Steps.Domain.Shared;

public static class LoadTemplatesCodes {
    public static readonly string NoTemplateBlock = StepCode.Of<LoadTemplatesCommand>(1);
    public static readonly string NoTemplateAssembly = StepCode.Of<LoadTemplatesCommand>(2);
    public static readonly string NoTemplateSet = StepCode.Of<LoadTemplatesCommand>(3);
}
