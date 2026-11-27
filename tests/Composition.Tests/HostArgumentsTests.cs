namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public sealed class HostArgumentsTests {
    [Test]
    public void The_base_host_gives_an_argument_to_the_host_arguments() {
        using var host = new TestApp().Build("--Probe:Value=42");

        Assert.That(host.Services.GetRequiredService<HostArguments>()["Probe:Value"], Is.EqualTo("42"));
    }

    [Test]
    public void A_module_reads_an_argument_of_the_base_host() {
        using var host = new TestApp(new ProbeModule()).Build("--Probe:Value=42");

        Assert.That(host.Services.GetRequiredService<Probe>().Value, Is.EqualTo("42"));
    }

    [Test]
    public void The_base_host_with_no_arguments_has_empty_host_arguments() {
        using var host = new TestApp(new ProbeModule()).Build();

        Assert.That(host.Services.GetRequiredService<HostArguments>().All, Is.Empty);
        Assert.That(host.Services.GetRequiredService<Probe>().Value, Is.Null);
    }

    [TestCase("-batchmode", "-logFile", "/tmp/a.log", "--Probe:Value=42")]
    [TestCase("/Users/x/Game.app", "--Probe:Value=42")]
    [TestCase("-key=value", "--Probe:Value=42")]
    [TestCase("word", "-flag", "value", "--Probe:Value", "42")]
    public void The_base_host_reads_only_the_double_dash_options(params string[] args) {
        using var host = new TestApp().Build(args);

        Assert.That(host.Services.GetRequiredService<HostArguments>().All,
            Is.EqualTo(new Dictionary<string, string> { ["Probe:Value"] = "42" }));
    }

    [Test]
    public void The_base_host_ignores_a_double_dash_option_with_no_value() {
        using var host = new TestApp().Build("--Flag", "--Other", "-x", "--Probe:Value", "-1", "--Last");

        Assert.That(host.Services.GetRequiredService<HostArguments>().All, Is.Empty);
    }

    [Test]
    public void A_key_is_not_case_sensitive() =>
        Assert.That(HostArguments.Read(["--Probe:Value=42"])["probe:value"], Is.EqualTo("42"));

    [Test]
    public void The_last_value_of_a_key_wins() =>
        Assert.That(HostArguments.Read(["--Name=first", "--name", "last"])["Name"], Is.EqualTo("last"));

    [Test]
    public void A_value_keeps_its_equals_signs_and_can_be_empty() {
        var arguments = HostArguments.Read(["--Query=a=b", "--Empty="]);

        Assert.That(arguments["Query"], Is.EqualTo("a=b"));
        Assert.That(arguments["Empty"], Is.Empty);
    }

    [Test]
    public void An_option_with_no_name_is_not_an_argument() =>
        Assert.That(HostArguments.Read(["--=value"]).All, Is.Empty);

    [Test]
    public void A_caller_cannot_change_the_values() =>
        Assert.That(HostArguments.Empty.All, Is.Not.InstanceOf<Dictionary<string, string>>());

    [Test]
    public void A_missing_key_has_no_value() =>
        Assert.That(HostArguments.Empty["Probe:Value"], Is.Null);

    public sealed record Probe(string? Value);

    public sealed class ProbeModule : Module {
        protected override void Configure(IServiceCollection services) =>
            services.AddSingleton(provider => new Probe(provider.GetRequiredService<HostArguments>()["Probe:Value"]));
    }
}
