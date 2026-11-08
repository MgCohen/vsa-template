using VSA.Contracts.Requests;

namespace Answers.Slices.GetItem;

public sealed record GetItemRequest(Guid Id, int Count, bool Loud) : IRequest<GetItemResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/items/{id}";
}
