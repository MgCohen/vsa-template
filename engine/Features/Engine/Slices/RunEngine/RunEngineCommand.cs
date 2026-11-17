namespace Engine.Slices.RunEngine;

using Report.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record RunEngineCommand(
    string Project,
    string Bin,
    string Block = "",
    ReportMode Mode = ReportMode.Error,
    string Skip = "",
    bool FailFast = false)
    : IConsoleCommand<ReportResult>;
