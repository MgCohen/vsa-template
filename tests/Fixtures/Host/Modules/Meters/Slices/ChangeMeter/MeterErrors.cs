using VSA.Contracts;

namespace Meters.Slices.ChangeMeter;

public static class MeterErrors {
    public static readonly Error NoChange = new("Meters.NoChange", "The delta is zero.");
}
