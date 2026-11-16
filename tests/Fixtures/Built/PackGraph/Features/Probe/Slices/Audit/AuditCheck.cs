using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Commands;

namespace PackGraph.Features.Probe.Slices.Audit;

public sealed class AuditCheck : BeforeCommandHook<AuditCommand> {
    public override Task<AuditCommand> BeforeAsync(IEnvelope<AuditCommand> envelope) => Task.FromResult(envelope.Message);
}
