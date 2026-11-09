using VSA.Contracts;
using VSA.Handlers.Requests;

namespace Shop.Slices.GetPrice;

sealed class GetPriceHandler : RequestHandler<GetPriceRequest, GetPriceResponse> {
    public override Task<Result<GetPriceResponse>> HandleAsync(Envelope<GetPriceRequest> envelope) =>
        Task.FromResult<Result<GetPriceResponse>>(new GetPriceResponse(new PriceDto(10m, "EUR")));
}
