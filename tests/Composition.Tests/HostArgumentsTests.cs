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

    [TestCase("-batchmode", "-logFile", "/tmp/a.log", "--Probe:Value=42")]
    [TestCase("/Users/x/Game.app", "--Probe:Value=42")]
    [TestCase("-key=value", "--Probe:Value=42")]
    [TestCase("word", "-flag", "value", "--Probe:Value", "42")]
    public void The_base_host_reads_only_the_double_dash_options(params string[] args) {
        using var host = new TestApp().Build(args);
        var configuration = host.Services.GetRequiredService<IConfiguration>();

        Assert.That(configuration.AsEnumerable().ToDictionary(pair => pair.Key, pair => pair.Value),
            Is.EqualTo(new Dictionary<string, string?> { ["Probe"] = null, ["Probe:Value"] = "42" }));
    }

    [Test]
    public void The_base_host_ignores_a_double_dash_option_with_no_value() {
        using var host = new TestApp().Build("--Flag", "--Other", "-x", "--Probe:Value", "-1", "--Last");

        Assert.That(host.Services.GetRequiredService<IConfiguration>().AsEnumerable(), Is.Empty);
    }

    public sealed record Probe(string? Value);

    public sealed class ProbeModule : Module {
        public override void Configure(IServiceCollection services) =>
            services.AddSingleton(provider => new Probe(provider.GetRequiredService<IConfiguration>()["Probe:Value"]));
    }
}
