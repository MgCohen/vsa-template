namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

public class TestApp<THost>(params Module[] modules) : App where THost : AppHost, ITestHost, new() {
    readonly List<Action<IServiceCollection>> configure = [];

    public override Kernel Kernel { get; } = new VsaKernel();

    public TestApp<THost> With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public override AppHost CreateHost() {
        var host = new THost { ModuleProvider = new FixedModules([.. modules]) };
        host.Actions.AddRange(configure);
        return host;
    }

    public TestBuild Build(params string[] args) {
        var host = CreateHost();
        return new TestBuild(host, host.Build(args, this));
    }

    public IServiceCollection Compose(IServiceCollection services, HostArguments? arguments = null) {
        var host = CreateHost();
        host.Compose(services, arguments ?? HostArguments.Empty, Kernel,
            new AppTypes(new FixedModules([.. modules])));
        return services;
    }
}
