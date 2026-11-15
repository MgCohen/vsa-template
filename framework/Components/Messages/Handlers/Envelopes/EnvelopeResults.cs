using VSA.Contracts;

namespace VSA.Handlers;

static class EnvelopeResults {
    internal const string FaultCode = "Fault";

    internal static Result<T> Of<T>(Envelope envelope) => envelope.State switch {
        EnvelopeState.Rejected => ((Result<T>)envelope.Error!).On(envelope),
        EnvelopeState.Faulted => ((Result<T>)new Error(FaultCode, envelope.Exception!.Message)).On(envelope),
        EnvelopeState.Accepted when envelope.Result is Result<T> result => result.On(envelope),
        EnvelopeState.Accepted => throw new InvalidOperationException(
            $"The handler of {envelope.Message.GetType().Name} returned "
            + $"{envelope.Result?.GetType().Name ?? "null"}, and the sender expects Result<{typeof(T).Name}>."),
        _ => throw new InvalidOperationException(
            $"Envelope {envelope.Id} is {envelope.State}, so it has no result."),
    };
}
