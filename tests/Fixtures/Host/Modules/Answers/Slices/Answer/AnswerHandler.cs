using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Answers.Slices.Answer;

sealed class AnswerHandler : RequestHandler<AnswerRequest, AnswerResponse> {
    public override Task<Result<AnswerResponse>> HandleAsync(Envelope<AnswerRequest> envelope) =>
        Task.FromResult<Result<AnswerResponse>>(envelope.Message.ErrorCode is { } code
            ? new Error(code, "The test error.")
            : new AnswerResponse("done"));
}
