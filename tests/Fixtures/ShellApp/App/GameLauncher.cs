namespace ShellApp.Host;

using Umpire.Vsa;
using Umpire.Kernels;

public sealed class GameLauncher(string name) : Launcher<Vsa<AppHost>> {
    public string Name => name;

    sealed class InnerApp : App<Vsa<AppHost>>;
}
