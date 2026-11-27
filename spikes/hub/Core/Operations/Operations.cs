namespace Spike.Core;

sealed class Operations(IEnumerable<IOperationReader> readers, IHub hub) : IOperations {
    readonly IOperationReader[] all = [.. readers];

    public Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        foreach (var reader in all) {
            if (reader.TryRead(operation, out var message)) {
                return hub.Send(message, ct);
            }
        }
        return Task.FromResult(MessageResult.Fail($"No reader knows the operation '{operation.Name}'."));
    }
}
