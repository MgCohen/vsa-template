namespace ShellApp.Host;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class GameLauncher(string name) : Launcher<VsaKernel> {
    public string Name => name;

    sealed class InnerApp : App<VsaKernel>;
}
