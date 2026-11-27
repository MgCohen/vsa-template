namespace Operations.Contracts.Requests;

using Operations.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public abstract record JobReport(ReportMode Mode, IReadOnlyList<Finding> Findings) : IResponse;
