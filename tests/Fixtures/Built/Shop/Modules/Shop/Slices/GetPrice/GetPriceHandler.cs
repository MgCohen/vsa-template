using VSA.Domain;
using VSA.Handlers.Requests;

namespace Shop.Slices.GetPrice;

sealed class GetPriceHandler : RoutedHandler<GetPriceRequest, GetPriceResponse> {
    protected override Task<Result<GetPriceResponse>> HandleRequestAsync(GetPriceRequest request, CancellationToken ct) =>
        Task.FromResult<Result<GetPriceResponse>>(new GetPriceResponse(new PriceDto(10m, "EUR")));
}
