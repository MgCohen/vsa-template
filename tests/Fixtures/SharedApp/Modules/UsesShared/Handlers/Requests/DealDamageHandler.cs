using Pipeline.Contracts.Requests;
using Pipeline.Handlers.Requests;
using UsesShared.Contracts.Requests;

namespace UsesShared.Handlers.Requests;

sealed class DealDamageHandler : MessageHandler<DealDamage> {
    public override MessageId Handle(DealDamage message) => new(message.Amount);
}
