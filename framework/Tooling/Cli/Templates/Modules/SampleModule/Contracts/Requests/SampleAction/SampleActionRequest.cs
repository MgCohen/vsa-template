using VSA.Contracts.Requests;

namespace SampleModule.Contracts.Requests.SampleAction;

// Add the input of the request as record parameters.
public sealed record SampleActionRequest : IRequest<SampleActionResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/sample-module/sample-action";
}
