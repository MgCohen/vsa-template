namespace Jobs.Slices.BuildJob;

using Operations.Contracts.Requests;
using Operations.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public sealed record BuildJobRequest(
    string Project,
    string Bin,
    string Block = "",
    ReportMode? Mode = null,
    string Skip = "",
    bool FailFast = false,
    string References = "",
    string SourceRoot = "",
    string IgnoredFolders = "",
    bool? ProjectChecks = null)
    : JobRequest(Project, Bin, Block, Mode, Skip, FailFast, References, SourceRoot, IgnoredFolders, ProjectChecks),
      IRequest<BuildJobReport> {
    public static string Name => "build";
}
