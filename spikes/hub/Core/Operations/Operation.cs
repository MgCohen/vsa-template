namespace Spike.Core;

public sealed record Operation(string Name, IReadOnlyDictionary<string, string> Values, object? Message = null);
