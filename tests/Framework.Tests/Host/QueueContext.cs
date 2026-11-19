namespace Framework.Tests.Host;

using System.Collections.Concurrent;

sealed class QueueContext : SynchronizationContext {
    readonly ConcurrentQueue<(SendOrPostCallback Work, object? State)> posts = new();

    public int Pending => posts.Count;

    public IDisposable Enter() {
        var before = Current;
        SetSynchronizationContext(this);
        return new Exit(before);
    }

    public override void Post(SendOrPostCallback d, object? state) => posts.Enqueue((d, state));

    public void RunPending() {
        while (posts.TryDequeue(out var post)) {
            post.Work(post.State);
        }
    }

    sealed class Exit(SynchronizationContext? before) : IDisposable {
        public void Dispose() => SetSynchronizationContext(before);
    }
}
