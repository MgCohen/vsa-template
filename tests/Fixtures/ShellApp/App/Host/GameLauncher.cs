namespace ShellApp.App.Host;

using Umpire.Vsa;
using Umpire.Kernels;

public sealed class GameLauncher(string name) : Launcher<Vsa<AppHost>> {
    public string Name => name;
}
