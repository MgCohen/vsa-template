using System.Reflection;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers;

namespace Steps.Infrastructure;

public sealed class MissingInput : BeforeStep<IEngineStep> {
    public const string Code = "VSA3004";

    public override Task<IEngineStep> BeforeAsync(IEnvelope<IEngineStep> envelope) {
        var step = envelope.Message;
        var nullability = new NullabilityInfoContext();
        var missing = step.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetValue(step) is null)
            .Where(p => nullability.Create(p).ReadState == NullabilityState.NotNull)
            .Select(p => p.Name)
            .ToList();
        if (missing.Count > 0) {
            envelope.Reject(new Error(Code, $"{StepNames.Of(step)} skipped: {string.Join(", ", missing)} is missing, because an earlier step failed."));
        }
        return Task.FromResult(step);
    }
}
