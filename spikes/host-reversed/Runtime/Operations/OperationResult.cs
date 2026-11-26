namespace Spike.Kernels;

public sealed record OperationResult(object? Value, string? Error, int ExitCode);
