namespace Spike.Core;

sealed class Operations(IOperationConverter? converter, IHub hub) : IOperations {
    public Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        if (converter is null) {
            return Task.FromResult(MessageResult.Fail($"No operation converter is registered, so '{operation.Name}' has no message."));
        }
        var converted = converter.Convert(operation);
        return converted.Value is { } message ? hub.Send(message, ct) : Task.FromResult(converted);
    }
}
