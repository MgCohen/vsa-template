using VSA.Contracts.Requests;

namespace SameRoute.Contracts.Requests;

public sealed record FirstRequest(Guid Id) : IRequest<FirstResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/same/{id}";
}

public sealed record FirstResponse(Guid Id);
