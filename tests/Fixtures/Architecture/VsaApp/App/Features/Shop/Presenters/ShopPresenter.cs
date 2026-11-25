namespace VsaApp.Features.Shop.Presenters;

using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Presenters;
using VsaApp.Features.Shop.Slices.Refund;

sealed class ShopPresenter(IRequestSender requests) : Presenter(requests) {
    static async Task Refund(IRequestSender sender) => await sender.SendAsync(new RefundRequest());
}
