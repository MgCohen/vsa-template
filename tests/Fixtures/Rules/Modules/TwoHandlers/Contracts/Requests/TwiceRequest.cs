using VSA.Contracts.Requests;

namespace TwoHandlers.Contracts.Requests;

public sealed record TwiceRequest(Guid Id) : IRequest<TwiceResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/two-handlers/{id}";
}

public sealed record TwiceResponse(Guid Id);
