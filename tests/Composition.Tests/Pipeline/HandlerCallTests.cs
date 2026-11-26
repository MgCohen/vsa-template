namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Events;

public sealed class HandlerCallTests {
    [TestCase(typeof(string))]
    [TestCase(typeof(EventHandlerCall<Rung>))]
    [TestCase(typeof(HandlerCall<,>))]
    [TestCase(typeof(AbstractCall<>))]
    [TestCase(typeof(CallWithParameter<>))]
    public void Calls_rejects_a_type_that_is_not_a_handler_call(Type notACall) {
        var error = Assert.Throws<ArgumentException>(() => new PipelineBuilder<Rung>().Calls(notACall));

        Assert.That(error!.Message, Does.StartWith($"{notACall.FullName} is not a handler call."));
    }

    [Test]
    public void A_send_to_a_handler_with_no_call_fails_with_the_name_of_the_handler() {
        using var services = Services();
        var pipeline = new PipelineBuilder<Ping>().Handlers(typeof(MarkerOnlyHandler)).Build(services);

        var error = Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.SendAsync<Unit>(new Ping()));

        Assert.That(error!.Message,
            Does.StartWith($"The pipeline has no call for {typeof(MarkerOnlyHandler).FullName}"));
    }

    [Test]
    public async Task An_event_handler_with_no_call_fails_and_the_next_handler_runs() {
        using var services = Services();
        var pipeline = new PipelineBuilder<Rung>()
            .Handlers(typeof(InterfaceRungHandler), typeof(BaseRungHandler))
            .Build(services);

        var result = await pipeline.SendAsync<Unit>(new Rung());

        Assert.That(result.Error?.Message, Does.StartWith("InterfaceRungHandler failed: The pipeline has no call"));
        Assert.That(services.GetRequiredService<BaseRungHandler>().Handled, Is.EqualTo(1));
    }

    [Test]
    public async Task CallsEventHandlers_calls_an_interface_event_handler() {
        using var services = Services();
        var pipeline = new PipelineBuilder<Rung>()
            .CallsEventHandlers()
            .Handlers(typeof(InterfaceRungHandler))
            .Build(services);

        var result = await pipeline.SendAsync<Unit>(new Rung());

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(services.GetRequiredService<InterfaceRungHandler>().Handled, Is.EqualTo(1));
    }

    [Test]
    public async Task AfterHandled_runs_after_an_error_result_and_not_after_a_throw() {
        using var services = Services();
        var pipeline = new PipelineBuilder<Rung>()
            .Handlers(typeof(ErrorRungHandler), typeof(ThrowingRungHandler))
            .AfterHandled<WatchedRungHandler>(handler => handler.AfterHandled++)
            .Build(services);

        await pipeline.SendAsync<Unit>(new Rung());

        Assert.That(services.GetRequiredService<ErrorRungHandler>().AfterHandled, Is.EqualTo(1));
        Assert.That(services.GetRequiredService<ThrowingRungHandler>().AfterHandled, Is.EqualTo(0));
    }

    static ServiceProvider Services() => new ServiceCollection()
        .AddSingleton(new AppTypes(new FixedModules()))
        .AddSingleton<InterfaceRungHandler>()
        .AddSingleton<BaseRungHandler>()
        .AddSingleton<ErrorRungHandler>()
        .AddSingleton<ThrowingRungHandler>()
        .BuildServiceProvider();

    public sealed record Ping;

    public sealed record Rung : IntegrationEvent;

    public sealed class MarkerOnlyHandler : IHandler<Ping>;

    public sealed class InterfaceRungHandler : IIntegrationEventHandler<Rung> {
        public int Handled { get; private set; }

        public Task<Result<Unit>> HandleAsync(IEnvelope<Rung> envelope) {
            Handled++;
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    public sealed class BaseRungHandler : IntegrationEventHandler<Rung> {
        public int Handled { get; private set; }

        public override Task<Result<Unit>> HandleAsync(IEnvelope<Rung> envelope) {
            Handled++;
            return Done;
        }
    }

    public abstract class WatchedRungHandler : IntegrationEventHandler<Rung> {
        public int AfterHandled { get; set; }
    }

    public sealed class ErrorRungHandler : WatchedRungHandler {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<Rung> envelope) =>
            Task.FromResult<Result<Unit>>(new Error("rung.error", "The handler failed."));
    }

    public sealed class ThrowingRungHandler : WatchedRungHandler {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<Rung> envelope) =>
            throw new InvalidOperationException("broken");
    }

    public abstract class AbstractCall<TMessage> : HandlerCall<IRunnableHandler<TMessage>, TMessage>
        where TMessage : class;

    public sealed class CallWithParameter<TMessage>(string name) : HandlerCall<IRunnableHandler<TMessage>, TMessage>
        where TMessage : class {
        public string Name { get; } = name;

        public override Task<IMessageResult> CallAsync(
            IRunnableHandler<TMessage> handler, IEnvelope<TMessage> envelope) =>
            handler.RunAsync(envelope);
    }
}
