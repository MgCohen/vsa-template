using Resources.Domain;
using VSA.Handlers.Commands;

namespace Resources.Slices.ChangeResource;

sealed class ChangeResourceHandler(Wallet wallet) : CommandHandler<ChangeResourceCommand, ChangeResourceResult> {
    public override Task<ChangeResourceResult> HandleAsync(ChangeResourceCommand command, CancellationToken ct) {
        var changed = wallet.TryChange(command.Resource, command.Delta, out var value);
        return Task.FromResult(new ChangeResourceResult(changed, value));
    }
}
