namespace Answers.Slices.ListAll;

using Umpire.Vsa.Contracts.Requests;

public sealed record ListAllRequest : IRequest<ListAllResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/all";
}
