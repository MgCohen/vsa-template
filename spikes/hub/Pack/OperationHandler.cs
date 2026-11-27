namespace Spike.Pack;

using Spike.Core;

public sealed class OperationHandler(RequestBinders binders, IHub hub) {
    public Task<MessageResult> Handle(Operation operation, CancellationToken ct) {
        if (!binders.TryGet(operation.Name, out var binder)) {
            return Task.FromResult(MessageResult.Fail($"No request has the name '{operation.Name}'."));
        }
        object request;
        try {
            request = binder.Bind(operation.Values);
        } catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException) {
            return Task.FromResult(MessageResult.Fail($"{operation.Name}: {e.Message}"));
        }
        return hub.Send(request, ct);
    }
}
