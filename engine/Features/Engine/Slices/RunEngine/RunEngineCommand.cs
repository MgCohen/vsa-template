using Report.Domain.Shared;
using VSA.Contracts.Commands;

namespace Engine.Slices.RunEngine;

public sealed record RunEngineCommand(
    string Root,
    string Bin,
    string Mode = "Error",
    string Skip = "",
    bool FailFast = false,
    bool UpdateBaseline = false)
    : IConsoleCommand<ReportResult>;
