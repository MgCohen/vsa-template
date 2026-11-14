using Microsoft.Extensions.DependencyInjection;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers;

namespace Steps.Infrastructure;

public sealed class RecordStep : AfterStep<IEngineStep> {
    public const string CouldNotRun = "VSA3000";

    public override Task AfterAsync(IEnvelope<IEngineStep> envelope) {
        envelope.Services.GetRequiredService<StepLog>().Add(Findings(envelope));
        return Task.CompletedTask;
    }

    static IEnumerable<Finding> Findings(IEnvelope<IEngineStep> envelope) {
        var step = StepNames.Of(envelope.Message);
        if (envelope is Envelope { Exception: { } exception }) {
            return [new Finding(CouldNotRun, FindingSeverity.Error, $"{step} could not run: {exception.Message}")];
        }
        if (envelope.Error is { Code: MissingInput.Code } skipped) {
            return [new Finding(skipped.Code, FindingSeverity.Info, skipped.Message)];
        }
        if (envelope.Error is { } error) {
            return [new Finding(error.Code, FindingSeverity.Error, error.Message)];
        }
        return envelope.Result?.Value is IStepResult result ? result.Findings : [];
    }
}
