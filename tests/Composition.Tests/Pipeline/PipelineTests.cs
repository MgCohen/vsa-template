namespace Composition.Tests.Pipeline;

using Arena;
using Answers;
using Answers.Slices.Answer;
using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Vsa.Composition;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;
using Umpire.Vsa.Presenters;
using Umpire.Kernel;

public sealed class PipelineTests {
    IHost host = null!;
    AsyncServiceScope scope;

    ICommandSender Sender => scope.ServiceProvider.GetRequiredService<ICommandSender>();
    IReadOnlyList<string> Log => host.Services.GetRequiredService<ArenaLog>().Lines;

    [SetUp]
    public void StartHost() {
        host = new TestApp(new ArenaModule()).Build();
        scope = host.Services.CreateAsyncScope();
    }

    [TearDown]
    public async Task StopHost() {
        await scope.DisposeAsync();
        host.Dispose();
    }

    [Test]
    public async Task A_run_goes_through_each_stage_in_order() {
        var result = await Sender.SendAsync(new StrikeCommand(5), default);

        Assert.That(result.Value, Is.EqualTo(4));
        var envelope = (Envelope<StrikeCommand>)result.Envelope!;
        Assert.That(envelope.State, Is.EqualTo(EnvelopeState.Accepted));
        Assert.That(envelope.Original.Damage, Is.EqualTo(5));
        Assert.That(envelope.Message.Damage, Is.EqualTo(4));
        Assert.That(Log, Is.EqualTo(new[] {
            "handle:Strike:4:arena", "after:StrikeCommand:Accepted:0:none", "audit:StrikeCommand:Accepted",
        }));
    }

    [Test]
    public async Task A_validator_rejects_the_message_before_the_handler() {
        var result = await Sender.SendAsync(new StrikeCommand(-1), default);

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error?.Code, Is.EqualTo("arena.negative"));
        Assert.That(result.Envelope?.State, Is.EqualTo(EnvelopeState.Rejected));
        Assert.That(Log, Is.EqualTo(new[] { "after:StrikeCommand:Rejected:0:none", "audit:StrikeCommand:Rejected" }));
    }

    [Test]
    public async Task A_send_from_a_handler_runs_inline_with_its_parent() {
        var result = await Sender.SendAsync(new ComboCommand(2, 3), default);

        Assert.That(result.Value, Is.EqualTo(4));
        string[] strike = ["handle:Strike:2:arena", "after:StrikeCommand:Accepted:1:ComboCommand", "audit:StrikeCommand:Accepted"];
        Assert.That(Log, Is.EqualTo(strike.Concat(strike)
            .Concat(["handle:Combo:4", "after:ComboCommand:Accepted:0:none", "audit:ComboCommand:Accepted"])));
    }

    [Test]
    public async Task A_send_from_an_After_hook_runs_inline() {
        var result = await Sender.SendAsync(new StrikeCommand(11), default);

        Assert.That(result.Value, Is.EqualTo(10));
        Assert.That(Log, Is.EqualTo(new[] {
            "handle:Strike:10:arena", "counter",
            "handle:Strike:0:arena", "after:StrikeCommand:Accepted:1:StrikeCommand", "audit:StrikeCommand:Accepted",
            "after:StrikeCommand:Accepted:0:none", "audit:StrikeCommand:Accepted",
        }));
    }

    [Test]
    public void A_fault_runs_the_after_stages_then_throws() {
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Sender.SendAsync(new FailCommand(), default));

        Assert.That(error!.Message, Is.EqualTo("The arena failed."));
        Assert.That(Log, Is.EqualTo(new[] { "after:FailCommand:Faulted:0:none", "audit:FailCommand:Faulted" }));
    }

    [Test]
    public async Task A_fault_that_the_runner_catches_gives_a_failed_result() {
        var pipeline = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>)).Runner<CatchRunner>()
            .Build(scope.ServiceProvider);

        var result = await pipeline.SendAsync<int>(new FailCommand());

        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Error, Is.EqualTo(new Error("Fault", "The arena failed.")));
        Assert.That(result.Envelope?.State, Is.EqualTo(EnvelopeState.Faulted));
    }

    [Test]
    public void A_synchronous_run_completes_at_once() {
        var inline = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>)).Build(scope.ServiceProvider);

        var sent = inline.SendAsync<int>(new StrikeCommand(3));

        Assert.That(sent.IsCompleted, Is.True);
        Assert.That(sent.Result.Value, Is.EqualTo(3));
    }

    [Test]
    public async Task A_custom_pipeline_with_no_handler_for_the_message_falls_back() {
        using var app = new TestApp(new ArenaModule())
            .With(services => services.AddPipeline(new PipelineBuilder<StrikeCommand>()))
            .Build();
        await using var appScope = app.Services.CreateAsyncScope();

        var result = await appScope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new StrikeCommand(5), default);

        Assert.That(result.Value, Is.EqualTo(4));
        Assert.That(app.Services.GetRequiredService<ArenaLog>().Lines, Does.Contain("audit:StrikeCommand:Accepted"));
    }

    [Test]
    public async Task A_pipeline_with_no_hook_base_runs_no_hook() {
        var pipeline = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>))
            .Build(scope.ServiceProvider);

        var result = await pipeline.SendAsync<int>(new StrikeCommand(5));

        Assert.That(result.Value, Is.EqualTo(5));
        Assert.That(Log, Is.EqualTo(new[] { "handle:Strike:5:none" }));
    }

    [Test]
    public void A_hook_base_is_a_type_that_implements_a_call_interface() {
        var error = Assert.Throws<ArgumentException>(
            () => new PipelineBuilder<ArenaMove>().HooksOf(typeof(CommandHandler<,>)));

        Assert.That(error!.Message, Does.StartWith("Umpire.Vsa.Handlers.Commands.CommandHandler`2 is not a hook base."));
    }

    [TestCase(typeof(BeforeStep<>))]
    [TestCase(typeof(AfterStep<>))]
    [TestCase(typeof(BeforeStep))]
    [TestCase(typeof(TagStep))]
    public void A_step_base_is_not_a_hook_base(Type stepBase) {
        var error = Assert.Throws<ArgumentException>(() => new PipelineBuilder<ArenaMove>().HooksOf(stepBase));

        Assert.That(error!.Message, Does.Contain("is not a hook base"));
    }

    [Test]
    public void A_step_is_added_one_time() {
        var builder = new PipelineBuilder<ArenaMove>().BeforeStep<TagStep>().AfterStep<AuditStep>();

        var before = Assert.Throws<InvalidOperationException>(() => builder.BeforeStep<TagStep>());
        var after = Assert.Throws<InvalidOperationException>(() => builder.AfterStep<AuditStep>());

        Assert.That(before!.Message, Does.StartWith("Arena.Slices.Duel.TagStep is already a before step"));
        Assert.That(after!.Message, Does.StartWith("Arena.Slices.Duel.AuditStep is already an after step"));
    }

    [TestCase(typeof(BeforeCommandHook<>))]
    [TestCase(typeof(AfterEventHook<>))]
    [TestCase(typeof(BeforeEventHook<>))]
    public void No_hook_base_takes_a_request(Type hookBase) {
        Assert.Throws<ArgumentException>(() => hookBase.MakeGenericType(typeof(AnswerRequest)));
    }

    [Test]
    public void The_After_command_hook_takes_no_request() {
        Assert.Throws<ArgumentException>(
            () => typeof(AfterCommandHook<,>).MakeGenericType(typeof(AnswerRequest), typeof(AnswerResponse)));
    }

    [Test]
    public async Task Two_kinds_with_one_message_base_have_their_own_pipelines() {
        using var app = new TestApp(new ArenaModule(), new AnswersModule()).Build();
        await using var appScope = app.Services.CreateAsyncScope();

        var answer = await appScope.ServiceProvider.GetRequiredService<IBus>().Send(new AnswerRequest(null));
        var strike = await appScope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new StrikeCommand(5), default);

        Assert.That(answer.Value, Is.EqualTo(new AnswerResponse("done")));
        Assert.That(strike.Value, Is.EqualTo(4));
    }

    [Test]
    public void A_pipeline_rejects_a_message_that_is_not_of_its_base() {
        var pipeline = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>))
            .Build(scope.ServiceProvider);

        var error = Assert.ThrowsAsync<ArgumentException>(() => pipeline.SendAsync<int>(new AnswerRequest(null)));

        Assert.That(error!.Message, Does.StartWith("Answers.Slices.Answer.AnswerRequest is not a ArenaMove"));
    }

    [Test]
    public void A_message_base_has_one_pipeline() {
        var services = new ServiceCollection().AddPipeline(new PipelineBuilder<ArenaMove>());

        var error = Assert.Throws<InvalidOperationException>(() => services.AddPipeline(new PipelineBuilder<ArenaMove>()));

        Assert.That(error!.Message, Does.StartWith("Two pipelines are registered for Arena.Slices.Duel.ArenaMove."));
    }

    sealed class CatchRunner : IRunner {
        public async Task RunAsync(Envelope envelope, Func<Envelope, Task> run) {
            try {
                await run(envelope);
            } catch (InvalidOperationException) {
                return;
            }
        }
    }
}
