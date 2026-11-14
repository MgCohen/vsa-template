using System.Reflection;
using Steps.Domain.Shared;
using VSA.Contracts;
using VSA.Handlers;

namespace Steps.Handlers;

public sealed class MissingInput : Validator<IEngineStep> {
    public const string Code = "VSA3004";

    public override Error? Validate(IEnvelope<IEngineStep> envelope) {
        var step = envelope.Message;
        var nullability = new NullabilityInfoContext();
        var missing = step.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0 && p.GetValue(step) is null)
            .Where(p => nullability.Create(p).ReadState == NullabilityState.NotNull)
            .Select(p => p.Name)
            .ToList();
        return missing.Count == 0
            ? null
            : new Error(Code, $"{StepNames.Of(step)} skipped: {string.Join(", ", missing)} is missing, because an earlier step failed.");
    }
}
