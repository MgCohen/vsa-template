using Umpire.Vsa.Contracts.Commands;

namespace PackGraph.Features.Probe.Slices.Audit;

public sealed record AuditCommand : ICommand<int>;
