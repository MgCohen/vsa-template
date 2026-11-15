using Report.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

namespace Report.Slices.WriteReport;

public sealed record WriteReportCommand(string Mode, IReadOnlyList<Finding> Findings) : ICommand<ReportResult>;
