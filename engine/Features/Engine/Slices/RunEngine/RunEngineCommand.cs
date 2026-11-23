namespace Engine.Slices.RunEngine;

using Report.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record RunEngineCommand(
    string Project,
    string Bin,
    string Block = "",
    string Mode = "Error",
    string Skip = "",
    bool FailFast = false,
    bool UpdateBaseline = false,
    string ScaffoldKind = "",
    string ScaffoldName = "",
    string ScaffoldGroups = "",
    string ScaffoldTemplates = "",
    string ScaffoldTemplatesBin = "")
    : IConsoleCommand<ReportResult>;
