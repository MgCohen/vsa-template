namespace Composition.Tests;

using Greet;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

[NonParallelizable]
public sealed class ConsoleHostTests {
    [Test]
    public void The_exit_code_comes_from_the_response_of_the_request() =>
        Assert.That(Run("--name", "Ann", "--code", "3"), Is.EqualTo(3));

    [Test]
    public void A_parameter_with_a_default_value_is_optional() =>
        Assert.That(Run("--name", "Ann"), Is.EqualTo(0));

    [Test]
    public void An_error_result_gives_exit_code_one() =>
        Assert.That(Run("--name", ""), Is.EqualTo(1));

    [Test]
    public void A_response_with_no_exit_code_gives_exit_code_zero() =>
        Assert.That(new TestApp<TestConsoleHost>(new QuietModule()).Run(["quiet", "--code", "7"]), Is.Zero);

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
    public void A_flags_enum_parameter_takes_a_list_of_member_names() =>
        Assert.That(Run("--name", "Ann", "--marks", "wave, Smile"), Is.EqualTo(23));

    [Test]
    public void A_flags_enum_parameter_takes_one_member_name() =>
        Assert.That(Run("--name", "Ann", "--marks", "Smile"), Is.EqualTo(22));

    [Test]
    public void A_flags_enum_list_with_a_name_that_is_not_a_member_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--marks", "Wave,Jump"), Is.EqualTo(1));

    [Test]
    public void An_enum_number_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "1"), Is.EqualTo(1));

    [Test]
    public void An_enum_number_that_is_not_a_member_gives_exit_code_one() =>
        Assert.That(Run("--name", "Ann", "--style", "7"), Is.EqualTo(1));

    [Test]
    public void An_app_with_two_requests_needs_a_name_on_the_command_line() {
        var app = new TestApp<TestConsoleHost>(new GreetModule(), new QuietModule());

        var error = Assert.Throws<InvalidOperationException>(() => app.Build("--name", "Ann"));

        Assert.That(error.Message, Does.StartWith("Name one request on the command line:"));
    }

    [Test]
    public void The_name_on_the_command_line_selects_the_request() =>
        Assert.That(TwoRequests().Run(["quiet", "--code", "7"]), Is.Zero);

    [Test]
    public void The_group_and_the_name_on_the_command_line_select_the_request() =>
        Assert.That(TwoRequests().Run(["greet", "greet", "--name", "Ann", "--code", "4"]), Is.EqualTo(4));

    static TestApp<TestConsoleHost> TwoRequests() => new(new GreetModule(), new QuietModule());

    static int Run(params string[] args) => new TestApp<TestConsoleHost>(new GreetModule()).Run(args);

    public sealed class QuietModule : Module;

    public sealed record QuietRequest(int Code) : IRequest<QuietResponse>;

    public sealed record QuietResponse(int Code) : IResponse;

    public sealed class QuietHandler : RequestHandler<QuietRequest, QuietResponse> {
        public override Task<Result<QuietResponse>> HandleAsync(Envelope<QuietRequest> envelope) =>
            Task.FromResult<Result<QuietResponse>>(new QuietResponse(envelope.Message.Code));
    }
}
