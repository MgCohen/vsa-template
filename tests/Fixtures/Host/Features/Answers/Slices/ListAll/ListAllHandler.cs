namespace Answers.Slices.ListAll;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class ListAllHandler : RequestHandler<ListAllRequest, ListAllResponse> {
    public override Task<Result<ListAllResponse>> HandleAsync(Envelope<ListAllRequest> envelope) =>
        Task.FromResult<Result<ListAllResponse>>(new ListAllResponse(3));
}
