namespace Engine.Slices.RunEngine;

using Report.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record RunEngineCommand(
    string Root,
    string Bin,
    string Mode = "Error",
    string Skip = "",
    bool FailFast = false,
    bool UpdateBaseline = false)
    : IConsoleCommand<ReportResult>;
