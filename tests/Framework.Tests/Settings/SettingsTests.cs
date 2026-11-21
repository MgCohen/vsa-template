namespace Framework.Tests.Settings;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Bare;
using Umpire.Config;
using Umpire.Kernels;
using Umpire.Vsa.Composition;

public sealed class SettingsTests {
    [Test]
    public void The_first_provider_that_answers_wins() {
        var module = new SampleModule();

        Compose([new NoAnswer(), Toml("[Settings]\nName = \"first\""), Toml("[Settings]\nName = \"second\"")], module);

        Assert.That(module.Settings?.Name, Is.EqualTo("first"));
    }

    [Test]
    public void With_no_answer_the_module_gets_the_default_settings() {
        var module = new SampleModule();

        Compose([new NoAnswer(), new UmpireConfigSettings(UmpireConfig.Empty)], module);

        Assert.That(module.Settings?.Name, Is.EqualTo("default"));
        Assert.That(module.Settings?.Size, Is.EqualTo(1));
    }

    [Test]
    public void Validate_stops_startup_with_all_errors_in_one_message() {
        var error = Assert.Throws<InvalidOperationException>(
            () => Compose([Toml("[Settings]\nName = \"bad\"\nSize = 20")], new SampleModule()));

        Assert.That(error!.Message,
            Is.EqualTo("The settings of the module Settings are not valid: Name must not be bad. Size must be 1 to 10."));
    }

    [Test]
    public void The_settings_are_in_the_services_and_given_to_Configure() {
        var module = new SampleModule();

        var services = Compose([Toml("[Settings]\nName = \"shelf\"\nSize = 3")], module);

        Assert.That(module.Settings?.Size, Is.EqualTo(3));
        Assert.That(services.BuildServiceProvider().GetRequiredService<SampleSettings>(), Is.SameAs(module.Settings));
    }

    [Test]
    public void A_plugin_gets_its_settings_from_the_table_of_its_name() {
        var plugin = new SamplePlugin();

        Compose([Toml("[Settings]\nName = \"tools\"")], plugin);

        Assert.That(plugin.Settings?.Name, Is.EqualTo("tools"));
    }

    [Test]
    public void A_settings_dictionary_binds() {
        var module = new SampleModule();

        Compose([Toml("[Settings]\nModes = { easy = 1, hard = 5 }")], module);

        Assert.That(module.Settings?.Modes, Is.EqualTo(new Dictionary<string, int> { ["easy"] = 1, ["hard"] = 5 }));
    }

    [Test]
    public void A_module_with_no_settings_composes_with_no_error() =>
        Assert.DoesNotThrow(() => Compose([], new BareModule()));

    [Test]
    public void Get_gives_the_settings_of_a_module() {
        var settings = new ModuleSettings([Toml("[Settings]\nName = \"read\"")]);

        Assert.That(settings.Get<SampleSettings>(new SampleModule()).Name, Is.EqualTo("read"));
    }

    static UmpireConfigSettings Toml(string text) => new(UmpireConfig.Parse(text));

    static ServiceCollection Compose(IReadOnlyList<ISettingsProvider> providers, Module module) {
        var services = new ServiceCollection();
        using var host = new SettingsHost(providers);
        host.Compose(services, new ConfigurationBuilder().Build(), new VsaKernel(),
            new AppTypes(new FixedModules(module), typeof(SettingsTests).Assembly));
        return services;
    }

    sealed class SettingsHost(IReadOnlyList<ISettingsProvider> providers) : AppHost {
        protected override IReadOnlyList<ISettingsProvider> Settings(AppTypes types) => providers;
    }

    sealed class NoAnswer : ISettingsProvider {
        public TSettings? Get<TSettings>(Module module) where TSettings : class, new() => null;
    }

    sealed class SamplePlugin : Plugin<SampleSettings> {
        public SampleSettings? Settings { get; private set; }

        public override void Configure(IServiceCollection services, SampleSettings settings) => Settings = settings;
    }

    public sealed class SampleSettings {
        public string Name { get; set; } = "default";

        public int Size { get; set; } = 1;

        public Dictionary<string, int> Modes { get; set; } = [];
    }

    sealed class SampleModule : Module<SampleSettings> {
        public SampleSettings? Settings { get; private set; }

        public override void Configure(IServiceCollection services, SampleSettings settings) => Settings = settings;

        protected override IEnumerable<string> Validate(SampleSettings settings) {
            if (settings.Name == "bad") {
                yield return "Name must not be bad.";
            }
            if (settings.Size is < 1 or > 10) {
                yield return "Size must be 1 to 10.";
            }
        }
    }
}
