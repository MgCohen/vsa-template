using VSA.Contracts.Requests;

namespace Good.Contracts.Requests;

public sealed record DeleteItemRequest(Guid Id) : IRequest<DeleteItemResponse> {
    public static HttpMethod Method => HttpMethod.Delete;
    public static string Route => "/good/items/{id}";
}

public sealed record DeleteItemResponse(Guid Id) : IResponse;
