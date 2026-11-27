namespace Spike.Core;

sealed class Operations(IEnumerable<IOperationReader> readers, IHub hub) : IOperations {
    readonly IOperationReader[] all = [.. readers];

    public Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        foreach (var reader in all) {
            object message;
            try {
                if (!reader.TryRead(operation, out message)) {
                    continue;
                }
            } catch (Exception e) when (e is ArgumentException or FormatException or InvalidCastException or OverflowException) {
                return Task.FromResult(MessageResult.Fail($"{operation.Name}: {e.Message}"));
            }
            return hub.Send(message, ct);
        }
        return Task.FromResult(MessageResult.Fail($"No reader knows the operation '{operation.Name}'."));
    }
}
