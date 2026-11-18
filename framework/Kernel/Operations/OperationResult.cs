namespace Umpire.Kernels;

using Umpire.Messages.Contracts;

public sealed record OperationResult(object? Value, Error? Error, int ExitCode);
