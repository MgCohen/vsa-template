namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Vsa;
using Umpire.Vsa.Definition;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

public sealed class ComposeOrderTests {
    [Test]
    public void Compose_runs_the_core_then_the_host_then_the_modules_in_order_then_the_app() {
        var services = new OrderApp(new LastModule(), new FirstModule()).Compose(new ServiceCollection());

        var steps = services.Select(service => service.ImplementationInstance).OfType<Step>().Select(step => step.Name);

        Assert.That(steps, Is.EqualTo(new[] { "host:core", "module:1", "module:2", "app" }));
    }

    public sealed record Step(string Name);

    public sealed class OrderHost : VsaHost {
        protected override void Install(IServiceCollection services, AppTypes types) {
            var core = services.Any(service => service.ImplementationInstance == types);
            services.AddSingleton(new Step(core ? "host:core" : "host"));
        }
    }

    public sealed class OrderApp(params Module[] modules) : App<Vsa<VsaArchitecture, OrderHost>> {
        public IServiceCollection Compose(IServiceCollection services) => Pack.CreateHost().Compose(services, this);

        protected override IReadOnlyList<Module> Modules() => modules;

        protected override void ConfigureServices(IServiceCollection services) => services.AddSingleton(new Step("app"));
    }

    public sealed class FirstModule : Module {
        protected override int Order => 1;

        public override void Configure(IServiceCollection services) => services.AddSingleton(new Step("module:1"));
    }

    public sealed class LastModule : Module {
        protected override int Order => 2;

        public override void Configure(IServiceCollection services) => services.AddSingleton(new Step("module:2"));
    }
}
