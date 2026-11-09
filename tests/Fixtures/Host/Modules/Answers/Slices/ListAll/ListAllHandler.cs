using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Answers.Slices.ListAll;

sealed class ListAllHandler : RequestHandler<ListAllRequest, ListAllResponse> {
    public override Task<Result<ListAllResponse>> HandleAsync(Envelope<ListAllRequest> envelope) =>
        Task.FromResult<Result<ListAllResponse>>(new ListAllResponse(3));
}
