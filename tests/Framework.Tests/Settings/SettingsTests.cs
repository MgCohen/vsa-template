namespace Framework.Tests.Settings;

using System.ComponentModel.DataAnnotations;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using Microsoft.Extensions.Configuration;
using Bare;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;
using Umpire.Vsa;
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
    public void AModuleWithNoConfigureComposesAndRegistersNothingOfItsOwn() {
        var services = Compose([], new BareModule());

        Assert.That(services.Select(service => service.ServiceType.Namespace), Has.None.StartWith("Bare"));
    }

    static ServiceCollection Configure(Dictionary<string, string?> values, SampleModule? module = null) =>
        Compose(values, module ?? new SampleModule());

    static ServiceCollection Compose(Dictionary<string, string?> values, Module module) {
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().AddInMemoryCollection(values).Build());
        var app = new ModuleApp(module);
        app.Pack.CreateHost().Compose(services, app);
        return services;
    }

    sealed class SamplePlugin : Plugin<SampleSettings> {
        public SampleSettings? Settings { get; private set; }

        public override void Configure(IServiceCollection services, SampleSettings settings) => Settings = settings;
    }

    sealed class ModuleApp(Module module) : App<Vsa<VsaHost>> {
        protected internal override IReadOnlyList<Module> Modules() => [module];
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
