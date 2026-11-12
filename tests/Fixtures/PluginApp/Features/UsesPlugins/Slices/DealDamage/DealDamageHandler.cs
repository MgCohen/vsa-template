using Pipeline.Contracts.Requests;
using Pipeline.Handlers.Requests;

namespace UsesPlugins.Slices.DealDamage;

// The router finds the handler kind through the plugin base: MessageHandler -> RequestHandler.
sealed class DealDamageHandler : MessageHandler<DealDamage> {
    public override MessageId Handle(DealDamage message) => new(message.Amount);
}
