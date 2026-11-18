namespace Umpire.Kernels;

public sealed class AppStop {
    readonly TaskCompletionSource<int> exit = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public bool IsRequested => exit.Task.IsCompleted;

    internal Task<int> Requested => exit.Task;

    public void Request(int exitCode = 0) => exit.TrySetResult(exitCode);
}
