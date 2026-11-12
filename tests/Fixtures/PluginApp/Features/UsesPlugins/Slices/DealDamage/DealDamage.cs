using Pipeline.Contracts.Requests;
using VSA.Contracts.Requests;

namespace UsesPlugins.Slices.DealDamage;

// A message of the module, on the plugin base of its layer.
public sealed record DealDamage(int Amount) : Message, IRequest<MessageId> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/damage";
}
