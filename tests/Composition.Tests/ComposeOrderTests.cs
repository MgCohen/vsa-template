namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class ComposeOrderTests {
    [Test]
    public void Compose_runs_the_kernel_then_the_modules_in_order_then_the_host() {
        var services = new TestApp(new Last.LastModule(), new First.FirstModule())
            .With(services => services.AddSingleton(new Step("host")))
            .Compose(new ServiceCollection());

        var steps = services.Select(service => service.ImplementationInstance).OfType<Step>().Select(step => step.Name);

        Assert.That(steps, Is.EqualTo(new[] { "module:1", "module:2", "host" }));
    }

    [Test]
    public void The_kernel_installs_the_core_before_the_architecture_layers() {
        var services = new TestApp().Compose(new ServiceCollection());

        var types = services.ToList().FindIndex(service => service.ServiceType == typeof(AppTypes));
        var stop = services.ToList().FindIndex(service => service.ServiceType == typeof(AppStop));
        var requests = services.ToList().FindIndex(service => service.ServiceType.Name == "IRequestSender");

        Assert.That(types, Is.LessThan(requests));
        Assert.That(stop, Is.LessThan(requests));
    }

    [Test]
    public void A_pack_variant_kernel_installs_the_layers_of_its_parent_first() {
        var services = new ServiceCollection();
        using var host = new TestHost();
        host.Compose(services, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
            new VariantKernel(), new AppTypes(new FixedModules(), typeof(ComposeOrderTests).Assembly));

        var requests = services.ToList().FindIndex(service => service.ServiceType.Name == "IRequestSender");
        var variant = services.ToList().FindIndex(s => s.ImplementationInstance is Step { Name: "variant" });

        Assert.That(requests, Is.LessThan(variant));
    }

    public sealed record Step(string Name);

    public sealed class VariantKernel : VsaKernel {
        protected override void Configure(IServiceCollection services, AppTypes types) {
            base.Configure(services, types);
            services.AddSingleton(new Step("variant"));
        }
    }
}
