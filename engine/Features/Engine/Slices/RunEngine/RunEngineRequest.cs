namespace Engine.Slices.RunEngine;

using Report.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public sealed record RunEngineRequest(
    string Project,
    string Bin,
    string Block = "",
    ReportMode Mode = ReportMode.Error,
    string Skip = "",
    bool FailFast = false)
    : IRequest<RunEngineResponse>;
