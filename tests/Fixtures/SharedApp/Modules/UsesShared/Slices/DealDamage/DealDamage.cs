using Pipeline.Slices.Messages;
using VSA.Contracts.Requests;

namespace UsesShared.Slices.DealDamage;

// A message of the module, on the Shared base of its layer.
public sealed record DealDamage(int Amount) : Message, IRequest<MessageId> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/damage";
}
