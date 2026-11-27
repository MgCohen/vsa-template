namespace Jobs.Slices.CompileJob;

using Operations.Contracts.Requests;
using Operations.Domain.Shared;

public sealed record CompileJobReport(ReportMode Mode, IReadOnlyList<Finding> Findings, bool InModel, bool ChecksTypes) : JobReport(Mode, Findings);
