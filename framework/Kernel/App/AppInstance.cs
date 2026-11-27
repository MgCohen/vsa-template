namespace Umpire.Kernels;

using System.Runtime.CompilerServices;

public sealed class AppInstance {
    readonly App app;
    readonly int run;

    internal AppInstance(App app, int run, Task<int> completion) {
        this.app = app;
        this.run = run;
        Completion = completion;
    }

    public Task<int> Completion { get; }

    public AppState State => app.StateOf(run);

    public void Stop(int exitCode = 0) => app.Stop(run, exitCode);

    public async ValueTask DisposeAsync() {
        Stop();
        await Completion.ConfigureAwait(false);
    }

    public TaskAwaiter<int> GetAwaiter() => Completion.GetAwaiter();
}
