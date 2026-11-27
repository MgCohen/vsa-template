namespace Operations.Contracts.Requests;

using Operations.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public abstract record JobRequest(
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
    string State = "",
    string Snapshots = "")
    : IRequest {
    public string Folder => Block.Length > 0 ? Block : Project;
}
