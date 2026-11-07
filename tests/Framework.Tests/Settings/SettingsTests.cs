using System.ComponentModel.DataAnnotations;
using RangeAttribute = System.ComponentModel.DataAnnotations.RangeAttribute;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace Framework.Tests.Settings;

public sealed class SettingsTests {
    [Test]
    public void EmptySettingsStopStartupWithOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure([]));

        Assert.That(error.Message, Is.EqualTo("Modules:Sample is not valid: The Name field is required."));
    }

    [Test]
    public void WrongValuesStopStartupWithAllErrorsInOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure(new() {
            ["Modules:Sample:Name"] = "bad",
            ["Modules:Sample:Size"] = "20",
        }));

        Assert.That(error.Message, Is.EqualTo("Modules:Sample is not valid: The field Size must be between 1 and 10. Name must not be bad."));
    }

    [Test]
    public void ValidSettingsAreRegisteredAndGivenToConfigure() {
        var module = new SampleModule();
        var services = Configure(new() { ["Modules:Sample:Name"] = "shelf", ["Modules:Sample:Size"] = "3" }, module);

        Assert.That(module.Settings?.Name, Is.EqualTo("shelf"));
        Assert.That(services.BuildServiceProvider().GetRequiredService<SampleSettings>(), Is.SameAs(module.Settings));
    }

    static ServiceCollection Configure(Dictionary<string, string?> values, SampleModule? module = null) {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        (module ?? new SampleModule()).Configure(new ModuleBuilder("Sample", new MicrosoftServices(services), configuration));
        return services;
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

        public override void Configure(ModuleBuilder module, SampleSettings settings) => Settings = settings;
    }
}
