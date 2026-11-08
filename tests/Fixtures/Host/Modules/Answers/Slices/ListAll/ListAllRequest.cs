using VSA.Contracts.Requests;

namespace Answers.Slices.ListAll;

public sealed record ListAllRequest : IRequest<ListAllResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/all";
}
