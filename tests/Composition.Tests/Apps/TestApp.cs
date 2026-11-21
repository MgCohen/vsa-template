namespace Composition.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public class TestApp<THost>(params Module[] modules) : App where THost : AppHost, ITestHost, new() {
    readonly List<Action<IServiceCollection>> configure = [];
    readonly TestPack<THost> pack = new(modules);

    public override Pack Pack => pack;

    public TestApp<THost> With(Action<IServiceCollection> services) {
        configure.Add(services);
        pack.Configure = configure;
        return this;
    }

    public TestBuild Build(params string[] args) {
        var host = Pack.CreateHost();
        return new TestBuild(host, host.Build(args, this));
    }

    public IServiceCollection Compose(IServiceCollection services, IConfiguration? configuration = null) {
        var host = Pack.CreateHost();
        host.Compose(services, configuration ?? new ConfigurationBuilder().Build(), Pack.Kernel,
            new AppTypes(new FixedModules([.. modules]), GetType().Assembly));
        return services;
    }
}
