namespace Spike.Core;

using Microsoft.Extensions.DependencyInjection;

sealed class Operations(IOperationConverter? converter, IServiceScopeFactory scopes) : IOperations {
    public async Task<MessageResult> Send(Operation operation, CancellationToken ct = default) {
        if (converter is null) {
            return MessageResult.Fail($"No operation converter is registered, so '{operation.Name}' has no message.");
        }
        var converted = converter.Convert(operation);
        if (converted.Value is not { } message) {
            return converted;
        }
        using var scope = scopes.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<IHub>().Send(message, ct).ConfigureAwait(false);
    }
}
