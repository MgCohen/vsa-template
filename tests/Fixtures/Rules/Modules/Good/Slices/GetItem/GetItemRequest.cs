using VSA.Contracts.Requests;

namespace Good.Slices.GetItem;

public sealed record GetItemRequest(Guid Id) : IRequest<GetItemResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/good/items/{id}";
}

public sealed record GetItemResponse(Guid Id) : IResponse;
