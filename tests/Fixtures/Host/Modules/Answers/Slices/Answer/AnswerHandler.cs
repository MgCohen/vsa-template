using VSA.Domain;
using VSA.Handlers.Requests;

namespace Answers.Slices.Answer;

sealed class AnswerHandler : RequestHandler<AnswerRequest, AnswerResponse> {
    protected override Task<Result<AnswerResponse>> HandleRequestAsync(AnswerRequest request, CancellationToken ct) =>
        Task.FromResult<Result<AnswerResponse>>(request.ErrorCode is { } code
            ? new Error(code, "The test error.")
            : new AnswerResponse("done"));
}
