namespace Spike.Kernels;

public sealed record Operation(string Name, Type Input, OperationInvoke Invoke);
