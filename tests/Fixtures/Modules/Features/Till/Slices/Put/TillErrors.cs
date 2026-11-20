namespace Till.Slices.Put;

using Umpire.Messages.Contracts;

public static class TillErrors {
    public static readonly Error Negative = new("Till.Negative", "The amount is negative.");
}
