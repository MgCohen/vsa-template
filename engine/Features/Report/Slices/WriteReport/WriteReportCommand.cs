namespace Report.Slices.WriteReport;

using Report.Domain.Shared;
using Steps.Domain.Shared;
using Umpire.Vsa.Contracts.Commands;

public sealed record WriteReportCommand(ReportMode Mode, IReadOnlyList<Finding> Findings) : ICommand<ReportResult>, IStepGroup {
    public static int Group => 6;
}
