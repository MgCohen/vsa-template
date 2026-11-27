using Spike.Core;

sealed class FailingHost : AppHost {
    public override Task Open(IOperations operations, AppStop stop) =>
        Task.Run(() => throw new InvalidOperationException("the host work broke"));
}
