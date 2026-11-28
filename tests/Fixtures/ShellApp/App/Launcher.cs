namespace ShellApp.Host;

using Umpire.Kernels;

public abstract class Launcher<TKernel> : IAppEntry<TKernel> where TKernel : Kernel, new() {
    readonly LauncherApp app = new();

    public IApp App => app;

    sealed class LauncherApp : App<TKernel>;
}
