namespace VsaApp.Features.Shop.Slices.Refund;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed record RefundRequest : IRequest<RefundResponse> {
    public static Effect Effect => Effect.Read;
}
