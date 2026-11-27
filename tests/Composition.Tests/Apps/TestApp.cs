namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class TestApp<THost>(params Module[] modules) : App where THost : AppHost, ITestHost, new() {
    readonly List<Action<IServiceCollection>> configure = [];
    volatile AppInstance? running;

    public override Kernel Kernel { get; } = new VsaKernel();

    public TestApp<THost> With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public THost CreateHost(params string[] args) {
        var host = new THost { ModuleProvider = new FixedModules([.. modules]), Line = args };
        host.Actions.AddRange(configure);
        return host;
    }

    public AppInstance? Running => running;

    public AppInstance Start(params string[] args) => running = Run(CreateHost(args));

    public int RunToEnd(params string[] args) => Start(args).Completion.GetAwaiter().GetResult();

    public void Stop() => running?.Stop();

    public TestBuild Build(params string[] args) {
        var host = CreateHost(args);
        return new TestBuild(host, host.Build(this));
    }

    public IServiceCollection Compose(IServiceCollection services, HostArguments? arguments = null) {
        var host = CreateHost();
        host.Compose(services, arguments ?? HostArguments.Empty, Kernel,
            new AppTypes(new FixedModules([.. modules])));
        return services;
    }
}
