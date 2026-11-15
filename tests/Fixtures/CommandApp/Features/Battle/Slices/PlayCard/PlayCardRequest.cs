namespace Battle.Slices.PlayCard;

using Umpire.Vsa.Contracts.Requests;

public sealed record PlayCardRequest(int Mana) : IRequest<PlayCardResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/cards/play";
}
