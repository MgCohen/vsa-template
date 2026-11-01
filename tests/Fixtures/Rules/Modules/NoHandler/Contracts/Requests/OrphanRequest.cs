using VSA.Contracts.Requests;

namespace NoHandler.Contracts.Requests;

public sealed record OrphanRequest(Guid Id) : IRequest<OrphanResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/no-handler/{id}";
}

public sealed record OrphanResponse(Guid Id);
