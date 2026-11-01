using VSA.Contracts.Requests;

namespace SameRoute.Contracts.Requests;

public sealed record SecondRequest(Guid Id) : IRequest<SecondResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/SAME/{key}/";
}

public sealed record SecondResponse(Guid Id);
