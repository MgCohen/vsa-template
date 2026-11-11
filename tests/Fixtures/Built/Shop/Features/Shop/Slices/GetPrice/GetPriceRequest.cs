using VSA.Contracts.Requests;

namespace Shop.Slices.GetPrice;

public sealed record GetPriceRequest(Guid ItemId) : IRequest<GetPriceResponse> {
    public static HttpMethod Method => HttpMethod.Get;
    public static string Route => "/api/shop/prices/{ItemId}";
}
