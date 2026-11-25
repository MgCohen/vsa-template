namespace VsaApp.Features.Shop.Domain.Shared;

public sealed class Discount(int percent) {
    public int Of(int amount) => amount * percent / 100;
}
