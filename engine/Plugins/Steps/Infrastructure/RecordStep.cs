namespace Steps.Infrastructure;

using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;

public sealed class RecordStep : AfterStep<IEngineStep> {
    public static readonly string CouldNotRun = RunCodes.CouldNotRun;

    public override Task AfterAsync(IEnvelope<IEngineStep> envelope) {
        envelope.Services.GetRequiredService<StepLog>().Add(Findings(envelope));
        return Task.CompletedTask;
    }

    static IEnumerable<Finding> Findings(IEnvelope<IEngineStep> envelope) {
        var step = StepNames.Of(envelope.Message);
        if (envelope is Envelope { Exception: { } exception }) {
            return [new Finding(CouldNotRun, FindingSeverity.Error, $"{step} could not run: {exception.Message}")];
        }
        if (envelope.Error is { } skipped && skipped.Code == MissingInput.Code) {
            return [new Finding(skipped.Code, FindingSeverity.Info, skipped.Message)];
        }
        if (envelope.Error is { } error) {
            return [new Finding(error.Code, FindingSeverity.Error, error.Message)];
        }
        return envelope.Result?.Value is IStepResult result ? result.Findings : [];
    }
}
