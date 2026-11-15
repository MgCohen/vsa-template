namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Kernel;

public class TestApp<THost>(params Module[] modules) : App<Vsa<THost>> where THost : VsaHost, new() {
    readonly List<Action<IServiceCollection>> configure = [];
    IKeySource keys = new ConstraintKeys();

    public TestApp<THost> With(Action<IServiceCollection> services) {
        configure.Add(services);
        return this;
    }

    public TestApp<THost> WithKeys(IKeySource source) {
        keys = source;
        return this;
    }

    public IHost Build(params string[] args) => Pack.CreateHost().Create(args, this);

    public IServiceCollection Compose(IServiceCollection services) => Pack.CreateHost().Compose(services, this);

    protected override IReadOnlyList<Module> Modules() => modules;

    protected override IKeySource Keys() => keys;

    protected override void ConfigureServices(IServiceCollection services) {
        foreach (var add in configure) {
            add(services);
        }
    }
}
