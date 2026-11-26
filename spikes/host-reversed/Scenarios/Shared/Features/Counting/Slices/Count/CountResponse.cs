namespace Greeter.Shared.Features.Counting.Slices.Count;

public sealed record CountResponse(int Count, string From) {
    public override string ToString() => $"count {Count} for {From}";
}
