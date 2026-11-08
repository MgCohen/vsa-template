using VSA.Contracts.Requests;

namespace Answers.Slices.Answer;

public sealed record AnswerRequest(string? ErrorCode) : IRequest<AnswerResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/answer";
}
