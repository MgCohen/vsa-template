namespace Composition.Tests;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class LocalApp<THost>(params Module[] modules) : App<VsaKernel>
    where THost : LocalHost, new() {
    AppInstance? running;

    public THost Host { get; private set; } = new() { ModuleProvider = new FixedModules([.. modules]) };

    public AppInstance Start() {
        Host = new THost { ModuleProvider = new FixedModules([.. modules]) };
        return running = Run(Host);
    }

    public void Stop() => running?.Stop();
}
