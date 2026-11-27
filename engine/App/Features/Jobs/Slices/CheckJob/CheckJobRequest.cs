namespace Jobs.Slices.CheckJob;

using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public sealed record CheckJobRequest(
    string Project,
    string Bin,
    string Block = "",
    ReportMode? Mode = null,
    string Skip = "",
    bool FailFast = false,
    string References = "",
    string SourceRoot = "",
    string IgnoredFolders = "",
    bool? ProjectChecks = null,
    bool Model = false,
    string State = "",
    string Snapshots = "")
    : JobRequest(Project, Bin, Block, Mode, Skip, FailFast, References, SourceRoot, IgnoredFolders, ProjectChecks, State, Snapshots),
      IRequest<CheckJobReport> {
    public static string Name => "check";
}
