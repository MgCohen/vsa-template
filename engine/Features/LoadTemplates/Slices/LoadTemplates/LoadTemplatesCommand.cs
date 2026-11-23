using LoadApp.Domain.Shared;
using LoadTemplates.Domain.Shared;
using Steps.Contracts.Commands;
using Steps.Domain.Shared;

namespace LoadTemplates.Slices.LoadTemplates;

public sealed record LoadTemplatesCommand(AppInput App, string Folder, string Bin)
    : EngineStep<TemplatesInput>, IStepGroup {
    public static int Group => 7;
}
