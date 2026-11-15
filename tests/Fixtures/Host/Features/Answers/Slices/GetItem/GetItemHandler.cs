using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

namespace Answers.Slices.GetItem;

sealed class GetItemHandler : RequestHandler<GetItemRequest, GetItemResponse> {
    public override Task<Result<GetItemResponse>> HandleAsync(Envelope<GetItemRequest> envelope) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(envelope.Message.Id, envelope.Message.Count, envelope.Message.Loud));
}
