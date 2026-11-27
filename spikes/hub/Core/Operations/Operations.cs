namespace Spike.Core;

sealed class Operations(IOperationConverter? converter, IHub hub) : IOperations {
    public Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        if (converter is null) {
            return Task.FromResult(MessageResult.Fail($"No operation converter is registered, so '{operation.Name}' has no message."));
        }
        object message;
        try {
            if (!converter.TryConvert(operation, out message)) {
                return Task.FromResult(MessageResult.Fail($"The converter knows no operation '{operation.Name}'."));
            }
        } catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException) {
            return Task.FromResult(MessageResult.Fail($"{operation.Name}: {e.Message}"));
        }
        return hub.Send(message, ct);
    }
}
