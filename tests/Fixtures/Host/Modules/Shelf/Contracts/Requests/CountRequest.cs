using VSA.Contracts.Requests;

namespace Shelf.Contracts.Requests;

public sealed record CountRequest : IRequest<CountResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/shelf/count";
}

public sealed record CountResponse(int Count);
