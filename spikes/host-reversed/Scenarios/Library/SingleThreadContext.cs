namespace Greeter.Library;

using System.Collections.Concurrent;

sealed class SingleThreadContext : SynchronizationContext {
    readonly BlockingCollection<Action> queue = new();
    bool finished;

    public override void Post(SendOrPostCallback d, object? state) => queue.Add(() => d(state));

    public override void Send(SendOrPostCallback d, object? state) =>
        throw new NotSupportedException("The probe context does not support Send.");

    public static string Run(Func<Task<string>> body, TimeSpan timeout) {
        var context = new SingleThreadContext();
        var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() => {
            SetSynchronizationContext(context);
            try {
                var task = body();
                task.ContinueWith(_ => context.Post(_ => context.finished = true, null), TaskScheduler.Default);
                while (!context.finished) {
                    context.queue.Take()();
                }
                done.TrySetResult(task.GetAwaiter().GetResult());
            } catch (Exception e) {
                done.TrySetException(e);
            }
        }) { IsBackground = true };
        thread.Start();
        return Task.WhenAny(done.Task, Task.Delay(timeout)).GetAwaiter().GetResult() == done.Task
            ? done.Task.GetAwaiter().GetResult()
            : $"no answer after {timeout.TotalSeconds} s: the context thread is blocked (deadlock)";
    }
}
