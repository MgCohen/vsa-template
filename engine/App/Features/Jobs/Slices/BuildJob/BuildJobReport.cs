namespace Jobs.Slices.BuildJob;

using Operations.Contracts.Requests;
using Operations.Domain.Shared;

public sealed record BuildJobReport(ReportMode Mode, IReadOnlyList<Finding> Findings) : JobReport(Mode, Findings);
