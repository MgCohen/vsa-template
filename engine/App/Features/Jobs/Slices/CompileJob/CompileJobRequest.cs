namespace Jobs.Slices.CompileJob;

using Facts.Domain.Shared.Compile;
using Umpire.Vsa.Contracts.Requests;

public sealed record CompileJobRequest(
    CompileProject Project, CompileStage Stage, CodeFile? File = null, CodeType? Type = null, bool HasErrors = false)
    : IRequest<CompileJobReport> {
    public static string Name => "compile";
}
