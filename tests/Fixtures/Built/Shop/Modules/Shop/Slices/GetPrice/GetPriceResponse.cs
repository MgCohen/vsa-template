using VSA.Contracts.Requests;

namespace Shop.Slices.GetPrice;

public sealed record GetPriceResponse(PriceDto Price) : IResponse;
