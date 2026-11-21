namespace Engine.Slices.RunEngine;

using Report.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public sealed record RunEngineRequest(
    string Project,
    string Bin,
    string Block = "",
    ReportMode? Mode = null,
    string Skip = "",
    bool FailFast = false,
    Stage Stage = Stage.All,
    string References = "",
    string SourceRoot = "",
    string IgnoredFolders = "",
    bool? ProjectChecks = null)
    : IRequest<RunEngineResponse>;
