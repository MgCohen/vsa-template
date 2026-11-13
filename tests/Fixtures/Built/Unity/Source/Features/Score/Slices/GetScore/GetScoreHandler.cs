using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Score.Slices.GetScore;

sealed class GetScoreHandler : RequestHandler<GetScoreRequest, GetScoreResponse> {
    public override Task<Result<GetScoreResponse>> HandleAsync(Envelope<GetScoreRequest> envelope) =>
        Task.FromResult<Result<GetScoreResponse>>(new GetScoreResponse(envelope.Message.Points * 2));
}
