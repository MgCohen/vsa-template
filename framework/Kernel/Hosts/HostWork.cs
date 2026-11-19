namespace Umpire.Kernels;

static class HostWork {
    static readonly AsyncLocal<bool> current = new();

    public static bool IsCurrent => current.Value;

    public static void MarkCurrentFlow() => current.Value = true;

    public static Task Run(Func<Task> work) => Task.Run(() => {
        MarkCurrentFlow();
        return work();
    });
}
