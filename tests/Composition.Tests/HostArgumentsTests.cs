namespace Composition.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class HostArgumentsTests {
    [Test]
    public void The_base_host_gives_an_argument_to_the_configuration() {
        using var host = new TestApp().Build("--Probe:Value=42");

        Assert.That(host.Services.GetRequiredService<IConfiguration>()["Probe:Value"], Is.EqualTo("42"));
    }

    [Test]
    public void A_module_reads_an_argument_of_the_base_host() {
        using var host = new TestApp(new ProbeModule()).Build("--Probe:Value=42");

        Assert.That(host.Services.GetRequiredService<Probe>().Value, Is.EqualTo("42"));
    }

    [Test]
    public void The_base_host_with_no_arguments_has_an_empty_configuration() {
        using var host = new TestApp(new ProbeModule()).Build();

        Assert.That(host.Services.GetRequiredService<IConfiguration>().AsEnumerable(), Is.Empty);
        Assert.That(host.Services.GetRequiredService<Probe>().Value, Is.Null);
    }

    public sealed record Probe(string? Value);

    public sealed class ProbeModule : Module {
        public override void Configure(IServiceCollection services) =>
            services.AddSingleton(provider => new Probe(provider.GetRequiredService<IConfiguration>()["Probe:Value"]));
    }
}
