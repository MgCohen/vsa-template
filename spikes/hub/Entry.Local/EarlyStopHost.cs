using Spike.Core;

sealed class EarlyStopHost : AppHost {
    public override Task Open(IOperations operations, AppStop stop) {
        stop.Request(7);
        return Task.CompletedTask;
    }
}
