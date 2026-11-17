namespace Composition.Tests;

using Greet;
using Umpire.Vsa.Composition;
using Umpire.Kernel;

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
    public void An_enum_parameter_takes_a_member_name_in_any_case() =>
        Assert.That(Run("--name", "Ann", "--style", "loud"), Is.EqualTo(10));

    [Test]
    public void An_enum_value_that_is_not_a_member_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "quiet"), Is.EqualTo(1));

    [Test]
    public void A_list_of_enum_members_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "Plain,Loud"), Is.EqualTo(1));

    [Test]
    public void An_enum_number_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "1"), Is.EqualTo(1));

    [Test]
    public void An_enum_number_that_is_not_a_member_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "7"), Is.EqualTo(1));

    [Test]
    public void An_app_with_no_entry_command_stops_the_build() {
        var app = new TestApp<ConsoleVsaHost>(new EchoModuleStub());

        var error = Assert.Throws<InvalidOperationException>(() => app.Build());

        Assert.That(error.Message, Does.StartWith("The console host found no entry command."));
    }

    static int Run(params string[] args) => new TestApp<ConsoleVsaHost>(new GreetModule()).Run(args);

    sealed class EchoModuleStub : Module;
}
