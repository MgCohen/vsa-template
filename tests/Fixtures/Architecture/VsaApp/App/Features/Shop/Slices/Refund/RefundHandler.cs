namespace VsaApp.Features.Shop.Slices.Refund;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Handlers.Requests;

sealed class RefundHandler : RequestHandler<RefundRequest, RefundResponse> {
    public override Task<Result<RefundResponse>> HandleAsync(IEnvelope<RefundRequest> envelope) =>
        Task.FromResult<Result<RefundResponse>>(new RefundResponse(1));
}
