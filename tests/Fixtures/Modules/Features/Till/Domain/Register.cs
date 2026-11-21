namespace Till.Domain;

using Till.Domain.Shared;

public sealed class Register(IStock<Coin> coins, IEnumerable<IStock<Gem>> gems, IExchange<Coin, Score> exchange) {
    public IStock<Coin> Coins => coins;

    public IReadOnlyList<IStock<Gem>> Gems { get; } = [.. gems];

    public IExchange<Coin, Score> Exchange => exchange;
}
