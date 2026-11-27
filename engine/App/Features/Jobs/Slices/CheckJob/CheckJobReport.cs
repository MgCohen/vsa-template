namespace Jobs.Slices.CheckJob;

using Operations.Contracts.Requests;
using Operations.Domain.Shared;

public sealed record CheckJobReport(ReportMode Mode, IReadOnlyList<Finding> Findings) : JobReport(Mode, Findings);
