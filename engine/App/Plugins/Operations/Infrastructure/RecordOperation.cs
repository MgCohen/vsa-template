namespace Operations.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Operations.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class RecordOperation : AfterStep<IJobOperation> {
    public static readonly string CouldNotRun = RunCodes.CouldNotRun;

    public override Task AfterAsync(IEnvelope<IJobOperation> envelope) {
        envelope.Services.GetRequiredService<OperationLog>().Add(Findings(envelope));
        return Task.CompletedTask;
    }

    static IEnumerable<Finding> Findings(IEnvelope<IJobOperation> envelope) {
        var step = OperationNames.Of(envelope.Message);
        if (envelope is Envelope { Exception: { } exception }) {
            return [new Finding(CouldNotRun, FindingSeverity.Error, $"{step} could not run: {exception.Message}")];
        }
        if (envelope.Error is { } error) {
            return [new Finding(error.Code, FindingSeverity.Error, error.Message)];
        }
        return envelope.Result?.Value is IOperationResult result ? result.Findings : [];
    }
}
