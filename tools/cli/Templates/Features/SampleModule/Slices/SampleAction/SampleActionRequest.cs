namespace SampleApp.Features.SampleModule.Slices.SampleAction;

using Umpire.Vsa.Contracts.Requests;

public sealed record SampleActionRequest : IRequest<SampleActionResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/api/sample-module/sample-action";
}
