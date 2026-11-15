namespace Meters.Slices.ChangeMeter;

using Umpire.Messages.Contracts;

public static class MeterErrors {
    public static readonly Error NoChange = new("Meters.NoChange", "The delta is zero.");
}
