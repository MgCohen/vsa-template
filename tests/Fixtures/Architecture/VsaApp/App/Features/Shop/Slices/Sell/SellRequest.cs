namespace VsaApp.Features.Shop.Slices.Sell;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public sealed class SellRequest : IRequest<SellResponse> {
    public static Effect Effect => Effect.Read;
}
