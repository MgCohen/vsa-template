namespace VsaApp.Features.Shop.Domain.Shared;

public sealed class Price {
    public const int Decimals = 2;
    public static readonly Price Free = new();
    public static int Count;

    public event Action? Changed;

    public int Amount { get; set; }
    public string Currency { get; init; } = "";

    public void Change() => Changed?.Invoke();

    public sealed class Tag {
        public string Text { get; private set; } = "";
    }
}
