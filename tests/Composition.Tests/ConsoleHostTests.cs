namespace Composition.Tests;

using Answers;
using Greet;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Composition;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Kernels;

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
    public void A_result_with_no_exit_code_gives_exit_code_zero() =>
        Assert.That(new TestApp<ConsoleVsaHost>(new QuietModule()).Run(["--code", "7"]), Is.Zero);

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
        var app = new TestApp<ConsoleVsaHost>(new AnswersModule());

        var error = Assert.Throws<InvalidOperationException>(() => app.Build());

        Assert.That(error.Message, Does.StartWith("The console host found no entry command."));
    }

    static int Run(params string[] args) => new TestApp<ConsoleVsaHost>(new GreetModule()).Run(args);

    public sealed class QuietModule : Module;

    public sealed record QuietCommand(int Code) : IConsoleCommand<QuietResult>;

    public sealed record QuietResult(int Code);

    public sealed class QuietHandler : CommandHandler<QuietCommand, QuietResult> {
        public override Task<Result<QuietResult>> HandleAsync(Envelope<QuietCommand> envelope) =>
            Task.FromResult<Result<QuietResult>>(new QuietResult(envelope.Message.Code));
    }
}
