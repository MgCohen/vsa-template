namespace VsaApp.Features.Shop.Domain.Shared;

public sealed record Amount(int Value) {
    public const int Max = 100;
    public static readonly Amount None = new(0);
    static readonly int[] Steps = [1, 10];

    public IReadOnlyList<int> Parts => [.. Steps.Select(step => Value * step)];

    public readonly record struct Rate(int Percent);

    public enum Kind { Net, Gross }
}
