using Steps.Domain.Shared;
using VSA.Contracts.Commands;

namespace Engine.Slices.RunEngine;

public sealed record RunEngineCommand(string Root, string Bin, string Mode = "Error", string Skip = "", bool FailFast = false)
    : IConsoleCommand<ReportResult>;
