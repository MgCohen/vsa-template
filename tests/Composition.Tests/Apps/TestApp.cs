namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Vsa;
using Umpire.Vsa.Composition;
using Umpire.Kernels;

public class TestApp<THost>(params Module[] modules) : App<Vsa<THost>> where THost : VsaHost, new() {
    readonly List<Action<IServiceCollection>> configure = [];

    public TestApp<THost> With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public IHost Build(params string[] args) => Pack.CreateHost().Create(args, this);

    public IServiceCollection Compose(IServiceCollection services) => Pack.CreateHost().Compose(services, this);

    protected internal override IReadOnlyList<Module> Modules() => modules;

    protected internal override void ConfigureServices(IServiceCollection services) {
        foreach (var add in configure) {
            add(services);
        }
    }
}
