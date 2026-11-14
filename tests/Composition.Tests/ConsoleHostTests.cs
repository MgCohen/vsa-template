using Greet;
using VSA.Composition;
using VSA.Kernel;

namespace Composition.Tests;

[NonParallelizable]
public sealed class ConsoleHostTests {
    [Test]
    public void The_exit_code_comes_from_the_result_of_the_entry_command() =>
        Assert.That(Run("--name", "Ann", "--code", "3"), Is.EqualTo(3));

    [Test]
    public void A_parameter_with_a_default_value_is_optional() =>
        Assert.That(Run("--name", "Ann"), Is.EqualTo(0));

    [Test]
    public void An_error_result_gives_exit_code_one() =>
        Assert.That(Run("--name", ""), Is.EqualTo(1));

    [Test]
    public void A_missing_parameter_gives_exit_code_one() =>
        Assert.That(Run(), Is.EqualTo(1));

    [Test]
    public void An_app_with_no_entry_command_stops_the_build() {
        var builder = AppBuilder.Create().Modules(new EchoModuleStub()).UseConsoleHost();

        var error = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.That(error.Message, Does.StartWith("The console host found no entry command."));
    }

    static int Run(params string[] args) {
        var builder = AppBuilder.Create(args).Modules(new GreetModule()).UseConsoleHost();
        var host = builder.SelectedHost();
        using var app = builder.Build();
        return host.Run(app);
    }

    sealed class EchoModuleStub : VSA.Module.Module {
        public override void Configure(AnchorBuilder module) {
        }
    }
}
