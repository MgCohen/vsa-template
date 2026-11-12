using Resources.Domain;
using VSA.Contracts;
using VSA.Handlers.Commands;

namespace Resources.Slices.ChangeResource;

sealed class ChangeResourceHandler(Wallet wallet) : CommandHandler<ChangeResourceCommand, ChangeResourceResult> {
    public override Task<Result<ChangeResourceResult>> HandleAsync(Envelope<ChangeResourceCommand> envelope) {
        var changed = wallet.TryChange(envelope.Message.Resource, envelope.Message.Delta, out var value);
        return Task.FromResult<Result<ChangeResourceResult>>(new ChangeResourceResult(changed, value));
    }
}
