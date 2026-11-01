using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace Framework.Tests.Settings;

public sealed class SettingsTests {
    [Fact]
    public void EmptySettingsStopStartupWithOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure([]));

        Assert.Equal("Modules:Sample is not valid: The Name field is required.", error.Message);
    }

    [Fact]
    public void WrongValuesStopStartupWithAllErrorsInOneMessage() {
        var error = Assert.Throws<InvalidOperationException>(() => Configure(new() {
            ["Modules:Sample:Name"] = "bad",
            ["Modules:Sample:Size"] = "20",
        }));

        Assert.Equal("Modules:Sample is not valid: The field Size must be between 1 and 10. Name must not be bad.", error.Message);
    }

    [Fact]
    public void ValidSettingsAreRegisteredAndGivenToConfigure() {
        var module = new SampleModule();
        var services = Configure(new() { ["Modules:Sample:Name"] = "shelf", ["Modules:Sample:Size"] = "3" }, module);

        Assert.Equal("shelf", module.Settings?.Name);
        Assert.Same(module.Settings, services.BuildServiceProvider().GetRequiredService<SampleSettings>());
    }

    static ServiceCollection Configure(Dictionary<string, string?> values, SampleModule? module = null) {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        (module ?? new SampleModule()).Configure(new ModuleBuilder("Sample", services, configuration));
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
