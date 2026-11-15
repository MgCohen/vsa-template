namespace Report.Slices.WriteReport;

using Report.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record WriteReportCommand(string Mode, IReadOnlyList<Finding> Findings) : ICommand<ReportResult>;
