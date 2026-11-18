namespace Umpire.Kernels;

using Umpire.Messages.Contracts;

public sealed record Operation(string Group, string Name, Effect Effect, Type Input, OperationInvoke Invoke);
