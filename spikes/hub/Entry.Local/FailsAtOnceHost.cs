using Spike.Core;

sealed class FailsAtOnceHost : AppHost {
    public override Task Open(IOperations operations, AppStop stop) =>
        Task.FromException(new InvalidOperationException("the host failed at once"));

    public override ValueTask Close() {
        Console.WriteLine("FailsAtOnceHost closed");
        return default;
    }
}
