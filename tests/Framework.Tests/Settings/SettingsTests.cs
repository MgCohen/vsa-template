namespace Framework.Tests.Settings;

using System.ComponentModel.DataAnnotations;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bare;
using Umpire.Kernels;
using Framework.Tests.Host;
using Umpire.Vsa.Composition;

public sealed class SettingsTests {
    [Test]
    public void EmptySettingsStopStartupWithOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure([]));

        Assert.That(error.Message, Is.EqualTo("Modules:Settings is not valid: The Name field is required."));
    }

    [Test]
    public void WrongValuesStopStartupWithAllErrorsInOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure(new() {
            ["Modules:Settings:Name"] = "bad",
            ["Modules:Settings:Size"] = "20",
        }));

        Assert.That(error.Message, Is.EqualTo("Modules:Settings is not valid: The field Size must be between 1 and 10. Name must not be bad."));
    }

    [Test]
    public void ValidSettingsAreRegisteredAndGivenToConfigure() {
        var module = new SampleModule();
        var services = Configure(new() { ["Modules:Settings:Name"] = "shelf", ["Modules:Settings:Size"] = "3" }, module);

        Assert.That(module.Settings?.Name, Is.EqualTo("shelf"));
        Assert.That(services.BuildServiceProvider().GetRequiredService<SampleSettings>(), Is.SameAs(module.Settings));
    }

    [Test]
    public void APluginWithSettingsGetsItsSettingsFromCompose() {
        var plugin = new SamplePlugin();
        Compose(new() { ["Modules:Settings:Name"] = "tools" }, plugin);

        Assert.That(plugin.Settings?.Name, Is.EqualTo("tools"));
    }

    [Test]
    public void AModuleWithNoConfigureComposesWithNoError() {
        Assert.DoesNotThrow(() => Compose([], new BareModule()));
    }

    [Test]
    public void TheHostGivesTheSettingsFromTheArgsOfCreate() {
        var module = new SampleModule();

        using var host = WebApps.OnTestServer(module).Build("--Modules:Settings:Name=from-args");

        Assert.That(module.Settings?.Name, Is.EqualTo("from-args"));
        Assert.That(host.Services.GetRequiredService<SampleSettings>(), Is.SameAs(module.Settings));
    }

    static ServiceCollection Configure(Dictionary<string, string?> values, SampleModule? module = null) =>
        Compose(values, module ?? new SampleModule());

    static ServiceCollection Compose(Dictionary<string, string?> values, Module module) {
        var services = new ServiceCollection();
        using var host = new AppHost();
        host.Compose(services, new ConfigurationBuilder().AddInMemoryCollection(values).Build(), new VsaKernel(),
            new AppTypes([module], typeof(SettingsTests).Assembly));
        return services;
    }

    sealed class SamplePlugin : Plugin<SampleSettings> {
        public SampleSettings? Settings { get; private set; }

        public override void Configure(IServiceCollection services, SampleSettings settings) => Settings = settings;
    }

    public sealed class SampleSettings {
        [Required]
        public string Name { get; init; } = "";

        [Range(1, 10)]
        public int Size { get; init; } = 1;
    }

    sealed class SampleModule : Module<SampleSettings> {
        public SampleSettings? Settings { get; private set; }

        protected override IEnumerable<string> Validate(SampleSettings settings) {
            if (settings.Name == "bad") {
                yield return "Name must not be bad.";
            }
        }

        public override void Configure(IServiceCollection services, SampleSettings settings) => Settings = settings;
    }
}
