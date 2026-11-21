namespace ShellApp.App.Host;

using Umpire.Kernels;

public abstract class Launcher<TPack> : IAppEntry<TPack> where TPack : Pack, new() {
    readonly LauncherApp app = new();

    public IApp App => app;

    sealed class LauncherApp : App<TPack>;
}
