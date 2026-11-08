using Pipeline.Contracts.Requests;
using Pipeline.Handlers.Requests;

namespace UsesShared.Slices.DealDamage;

// The router finds the handler kind through the Shared base: MessageHandler -> RoutedHandler.
sealed class DealDamageHandler : MessageHandler<DealDamage> {
    public override MessageId Handle(DealDamage message) => new(message.Amount);
}
