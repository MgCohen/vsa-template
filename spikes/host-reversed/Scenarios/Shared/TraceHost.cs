namespace Greeter.Shared;

using Spike.Kernels;

public sealed class TraceHost(string name, int openDelayMs = 0) : AppHost {
    public override void Open(IOperations operations, AppStop stop) {
        Thread.Sleep(openDelayMs);
        Console.WriteLine($"[{name}] open, operations: {string.Join(", ", operations.All.Select(o => o.Name))}");
    }

    public override void Dispose() {
        Console.WriteLine($"[{name}] dispose");
        base.Dispose();
    }
}
