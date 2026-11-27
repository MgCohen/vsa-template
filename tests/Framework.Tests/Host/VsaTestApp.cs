namespace Framework.Tests.Host;

using Umpire.Kernels;
using Umpire.Vsa.Composition;

abstract class VsaTestApp(Func<AppHost> create) : App {
    AppInstance? running;

    public override Kernel Kernel { get; } = new VsaKernel();

    public AppHost CreateHost() => create();

    public AppInstance Start() => running = Run(create());

    public int RunToEnd() => Start().Completion.GetAwaiter().GetResult();

    public void Stop() => running?.Stop();
}
