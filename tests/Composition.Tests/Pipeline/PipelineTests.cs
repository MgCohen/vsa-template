using Arena;
using Arena.Slices.Duel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VSA.Composition;
using VSA.Contracts;
using VSA.Contracts.Commands;
using VSA.Handlers;
using VSA.Handlers.Commands;

namespace Composition.Tests.Pipeline;

// The Arena fixture sends its moves through a custom pipeline: StackRunner, TagStep before, AuditStep after.
// Its slice has a validator, a Before event (ArmorRule takes 1 damage) and two After events.
public sealed class PipelineTests {
    IHost host = null!;
    AsyncServiceScope scope;

    ICommandSender Sender => scope.ServiceProvider.GetRequiredService<ICommandSender>();
    IReadOnlyList<string> Log => host.Services.GetRequiredService<ArenaLog>().Lines;

    [SetUp]
    public void StartHost() {
        host = VsaBuilder.Create().UseGenericHost().Modules(new ArenaModule()).Build();
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
    public async Task A_send_from_an_After_event_waits_on_the_stack() {
        var result = await Sender.SendAsync(new StrikeCommand(11), default);

        Assert.That(result.Value, Is.EqualTo(10));
        Assert.That(Log, Is.EqualTo(new[] {
            "handle:Strike:10:arena", "after:StrikeCommand:Accepted:0:none", "counter", "audit:StrikeCommand:Accepted",
            "handle:Strike:0:arena", "after:StrikeCommand:Accepted:1:StrikeCommand", "audit:StrikeCommand:Accepted",
        }));
    }

    [Test]
    public void A_fault_runs_the_after_stages_then_throws() {
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Sender.SendAsync(new FailCommand(), default));

        Assert.That(error!.Message, Is.EqualTo("The arena failed."));
        Assert.That(Log, Is.EqualTo(new[] { "after:FailCommand:Faulted:0:none", "audit:FailCommand:Faulted" }));
    }

    [Test]
    public async Task Each_build_is_a_new_instance_with_its_own_queue() {
        var builder = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>)).Runner<QueuedRunner>();
        var first = builder.Build(scope.ServiceProvider);
        var second = builder.Build(scope.ServiceProvider);

        var firstSent = first.SendAsync<int>(new StrikeCommand(3));
        var secondSent = second.SendAsync<int>(new StrikeCommand(3));
        Assert.That(firstSent.IsCompleted, Is.False);

        await first.StartAsync();

        Assert.That((await firstSent).Value, Is.EqualTo(2));
        Assert.That(secondSent.IsCompleted, Is.False);
        await second.StartAsync();
        Assert.That((await secondSent).Value, Is.EqualTo(2));
    }

    [Test]
    public void A_synchronous_run_completes_at_once() {
        var inline = new PipelineBuilder<ArenaMove>().HandlersOf(typeof(CommandHandler<,>)).Build(scope.ServiceProvider);

        var sent = inline.SendAsync<int>(new StrikeCommand(3));

        Assert.That(sent.IsCompleted, Is.True);
        Assert.That(sent.Result.Value, Is.EqualTo(2));
    }

    [Test]
    public void The_module_instance_is_one_service() {
        var pipeline = host.Services.GetRequiredService<IPipeline<ArenaMove>>();

        Assert.That(pipeline, Is.SameAs(host.Services.GetRequiredService<Pipelines>().Of<ArenaMove>()));
        Assert.That(pipeline.Runner, Is.TypeOf<StackRunner>());
    }

    [Test]
    public void A_message_base_has_one_pipeline() {
        var services = new ServiceCollection().AddPipeline(new PipelineBuilder<ArenaMove>());

        var error = Assert.Throws<InvalidOperationException>(() => services.AddPipeline(new PipelineBuilder<ArenaMove>()));

        Assert.That(error!.Message, Does.StartWith("Two pipelines are registered for Arena.Slices.Duel.ArenaMove."));
    }
}
