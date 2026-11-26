namespace Spike.Kernels;

public sealed class AppStop {
    Action<int>? listener;

    public void Request(int exitCode = 0) => listener?.Invoke(exitCode);

    internal void Listen(Action<int> listener) => this.listener = listener;
}
