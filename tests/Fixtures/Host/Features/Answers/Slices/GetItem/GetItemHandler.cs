namespace Answers.Slices.GetItem;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class GetItemHandler : RequestHandler<GetItemRequest, GetItemResponse> {
    public override Task<Result<GetItemResponse>> HandleAsync(Envelope<GetItemRequest> envelope) =>
        Task.FromResult<Result<GetItemResponse>>(new GetItemResponse(envelope.Message.Id, envelope.Message.Count, envelope.Message.Loud));
}
