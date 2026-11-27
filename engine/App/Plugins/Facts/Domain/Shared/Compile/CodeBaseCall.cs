namespace Facts.Domain.Shared.Compile;

public sealed record CodeBaseCall(
    string Type, string Method, string Called, bool IsOverride, IReadOnlyList<string> Bases, CodePlace Place);
