namespace VsaApp.Features.Shop.Slices.Refund;

using Umpire.Vsa.Contracts.Requests;

public readonly record struct RefundResponse(int Amount) : IResponse;
