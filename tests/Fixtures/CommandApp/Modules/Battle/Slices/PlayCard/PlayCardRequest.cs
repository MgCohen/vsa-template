using VSA.Contracts.Requests;

namespace Battle.Slices.PlayCard;

public sealed record PlayCardRequest(int Mana) : IRequest<PlayCardResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/cards/play";
}
