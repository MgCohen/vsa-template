namespace Answers.Slices.Answer;

using Umpire.Vsa.Contracts.Requests;

public sealed record AnswerRequest(string? ErrorCode) : IRequest<AnswerResponse> {
    public static HttpMethod Method => HttpMethod.Post;
    public static string Route => "/answer";
}
