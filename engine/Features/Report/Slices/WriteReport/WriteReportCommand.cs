using Steps.Domain.Shared;
using VSA.Contracts.Commands;

namespace Report.Slices.WriteReport;

public sealed record WriteReportCommand(string Mode, IReadOnlyList<Finding> Findings) : ICommand<ReportResult>;
