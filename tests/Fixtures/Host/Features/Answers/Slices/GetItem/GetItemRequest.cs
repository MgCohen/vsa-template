namespace Answers.Slices.GetItem;

using Umpire.Vsa.Contracts.Requests;

public sealed record GetItemRequest(Guid Id, int Count, bool Loud) : IRequest<GetItemResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/items/{id}";
}
