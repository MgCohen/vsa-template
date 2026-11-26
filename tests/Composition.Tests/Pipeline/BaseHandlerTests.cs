namespace Composition.Tests.Pipeline;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Markers;
using Umpire.Messages.Contracts;
using Umpire.Messages.Handlers;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Umpire.Vsa.Handlers.Commands;
using Umpire.Vsa.Handlers.Events;

public sealed class BaseHandlerTests {
    readonly ServiceProvider services = new ServiceCollection().AddSingleton<Calls>().BuildServiceProvider();

    Calls Log => services.GetRequiredService<Calls>();

    [OneTimeTearDown]
    public void Dispose() => services.Dispose();

    [Test]
    public async Task A_derived_command_goes_to_the_handler_of_its_base() {
        var pipeline = Commands(typeof(ShapeHandler));

        var result = await pipeline.SendAsync<string>(new Circle());

        Assert.That(result.Value, Is.EqualTo("shape:Circle"));
    }

    [Test]
    public async Task The_handler_of_the_exact_type_wins_over_the_handler_of_a_base() {
        var pipeline = Commands(typeof(ShapeHandler), typeof(SquareHandler));

        var square = await pipeline.SendAsync<string>(new Square());
        var circle = await pipeline.SendAsync<string>(new Circle());

        Assert.That(square.Value, Is.EqualTo("square"));
        Assert.That(circle.Value, Is.EqualTo("shape:Circle"));
    }

    [Test]
    public async Task The_handler_of_the_nearest_base_wins() {
        var pipeline = Commands(typeof(ShapeHandler), typeof(RoundHandler));

        var result = await pipeline.SendAsync<string>(new Ball());

        Assert.That(result.Value, Is.EqualTo("round:Ball"));
    }

    [Test]
    public void Two_handlers_at_the_same_distance_stop_the_send_and_name_both() {
        var pipeline = Commands(typeof(ShapeHandler), typeof(PaintedHandler), typeof(RoundHandler));

        var error = Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.SendAsync<string>(new PaintedBall()));

        Assert.That(error!.Message, Does.Contain(nameof(PaintedHandler)).And.Contain(nameof(RoundHandler)));
    }

    [Test]
    public async Task The_message_closes_an_open_handler_with_its_result_type() {
        var pipeline = new PipelineBuilder<JsCall>().CallsTestHandlers().Handlers(typeof(JsHandler<>)).Build(services);

        var wait = await pipeline.SendAsync<Waited>(new Wait("panel"));
        var frames = await pipeline.SendAsync<int>(new Frames(3));

        Assert.That(wait.Value, Is.EqualTo(new Waited("Wait")));
        Assert.That(frames.Value, Is.EqualTo(0));
        Assert.That(Log.Lines, Does.Contain("js:Wait").And.Contain("js:Frames"));
    }

    [Test]
    public async Task An_event_runs_the_handlers_of_its_type_and_of_each_base_one_time() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers()
            .Handlers(typeof(ItemChangedHandler), typeof(CardChangedHandler), typeof(BothHandler))
            .Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new CardChanged());

        Assert.That(Log.Lines, Is.EqualTo(new[] { "card", "both:card", "item" }));
    }

    [Test]
    public async Task An_event_calls_a_handler_of_many_bases_one_time_for_its_nearest_base() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers()
            .Handlers(typeof(ItemChangedHandler), typeof(CardChangedHandler), typeof(BothHandler))
            .Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new SubCard());

        Assert.That(Log.Lines, Is.EqualTo(new[] { "card", "both:card", "item" }));
    }

    [Test]
    public async Task An_event_runs_one_closure_of_an_open_handler_when_a_closed_one_also_matches() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers()
            .Handlers(typeof(Audit<ItemChanged>), typeof(Audit<>))
            .Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new CardChanged());

        Assert.That(Log.Lines, Is.EqualTo(new[] { "audit:CardChanged" }));
    }

    [Test]
    public async Task The_message_closes_an_open_validator_and_hook_one_time_next_to_a_type_argument_closure() {
        using var typed = new ServiceCollection()
            .AddSingleton<Calls>()
            .AddSingleton(new AppTypes(new FixedModules(new CheckModule())))
            .BuildServiceProvider();
        var pipeline = new PipelineBuilder<Check>().CallsTestHandlers()
            .Handlers(typeof(CheckHandler))
            .HooksOf(typeof(AfterCommandHook<,>))
            .Build(typed);

        var result = await pipeline.SendAsync<string>(new SubCheck());

        Assert.That(result.Value, Is.EqualTo("ok"));
        Assert.That(typed.GetRequiredService<Calls>().Lines, Is.EqualTo(new[] { "valid:SubCheck", "hook:SubCheck" }));
    }

    [Test]
    public async Task An_event_runs_the_handler_of_a_derived_type_before_the_handler_of_its_base() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers()
            .Handlers(typeof(BaseChangedHandler), typeof(ZoneMovedHandler))
            .Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new ZoneMoved());

        Assert.That(Log.Lines, Is.EqualTo(new[] { "zone", "base" }));
    }

    [Test]
    public async Task An_event_runs_the_handler_of_an_interface_that_it_implements() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers()
            .Handlers(typeof(ItemChangedHandler), typeof(StampHandler))
            .Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new StampedItem());

        Assert.That(Log.Lines, Is.EqualTo(new[] { "stamp", "item" }));
    }

    [Test]
    public async Task The_message_closes_an_open_handler_of_a_nested_pattern() {
        var pipeline = new PipelineBuilder<Call>().CallsTestHandlers().Handlers(typeof(ListCallHandler<>)).Build(services);

        var result = await pipeline.SendAsync<string>(new Call<List<int>>());

        Assert.That(result.Value, Is.EqualTo("list:Int32"));
    }

    [Test]
    public void A_command_with_two_shapes_of_an_open_pattern_stops_the_send_and_names_both() {
        var pipeline = new PipelineBuilder<Call>().CallsTestHandlers().Handlers(typeof(WrapHandler<>)).Build(services);

        var error = Assert.ThrowsAsync<InvalidOperationException>(
            () => pipeline.SendAsync<string>(new DoubleWrap()));

        Assert.That(error!.Message, Does.Contain("two nearest handlers")
            .And.Contain("WrapHandler`1[[System.Int32").And.Contain("WrapHandler`1[[System.String"));
    }

    [Test]
    public async Task An_event_with_two_shapes_of_an_open_pattern_runs_each_closure() {
        var pipeline = new PipelineBuilder<IntegrationEvent>().CallsTestHandlers().Handlers(typeof(WrapAudit<>)).Build(services);
        Log.Lines.Clear();

        await pipeline.SendAsync<Unit>(new DoubleWrapped());

        Assert.That(Log.Lines, Is.EquivalentTo(new[] { "wrap:Int32", "wrap:String" }));
    }

    [Test]
    public void An_open_handler_with_a_parameter_outside_its_message_stops_the_build() {
        var error = Assert.Throws<InvalidOperationException>(
            () => new PipelineBuilder<Call>().CallsTestHandlers().Handlers(typeof(ExtraHandler<,>)).Build(services));

        Assert.That(error!.Message, Does.Contain("ExtraHandler<T, TExtra>").And.Contain("type parameters"));
    }

    Pipeline Commands(params Type[] handlers) =>
        new PipelineBuilder<Shape>().CallsTestHandlers().Handlers(handlers).Build(services);

    public sealed class Calls {
        public List<string> Lines { get; } = [];
    }

    public abstract record Shape : ICommand<string>;

    public record Circle : Shape;

    public sealed record Square : Shape;

    public record Round : Shape;

    public record Ball : Round;

    public interface IPainted;

    public sealed record PaintedBall : Ball, IPainted;

    public sealed class ShapeHandler : CommandHandler<Shape, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Shape> envelope) =>
            Task.FromResult<Result<string>>($"shape:{envelope.Message.GetType().Name}");
    }

    public sealed class SquareHandler : CommandHandler<Square, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Square> envelope) =>
            Task.FromResult<Result<string>>("square");
    }

    public sealed class RoundHandler : CommandHandler<Ball, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Ball> envelope) =>
            Task.FromResult<Result<string>>($"round:{envelope.Message.GetType().Name}");
    }

    public sealed class PaintedHandler : ITestHandler<IPainted> {
        public Task<IMessageResult> RunAsync(IEnvelope<IPainted> envelope) =>
            Task.FromResult<IMessageResult>((Result<string>)"painted");
    }

    public abstract record JsCall;

    public abstract record JsCall<TResult> : JsCall, ICommand<TResult>;

    public sealed record Wait(string Target) : JsCall<Waited>;

    public sealed record Waited(string Function);

    public sealed record Frames(int Count) : JsCall<int>;

    public sealed class JsHandler<TResult>(Calls calls) : CommandHandler<JsCall<TResult>, TResult> {
        public override Task<Result<TResult>> HandleAsync(IEnvelope<JsCall<TResult>> envelope) {
            var function = envelope.Message.GetType().Name;
            calls.Lines.Add($"js:{function}");
            return Task.FromResult<Result<TResult>>(
                envelope.Message is Wait ? (TResult)(object)new Waited(function) : default!);
        }
    }

    public record ItemChanged : IntegrationEvent;

    public record CardChanged : ItemChanged;

    public sealed record SubCard : CardChanged;

    public sealed class ItemChangedHandler(Calls calls) : IntegrationEventHandler<ItemChanged> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<ItemChanged> envelope) {
            calls.Lines.Add("item");
            return Done;
        }
    }

    public sealed class CardChangedHandler(Calls calls) : IntegrationEventHandler<CardChanged> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<CardChanged> envelope) {
            calls.Lines.Add("card");
            return Done;
        }
    }

    public sealed class BothHandler(Calls calls)
        : IIntegrationEventHandler<ItemChanged>, IIntegrationEventHandler<CardChanged> {
        public Task<Result<Unit>> HandleAsync(IEnvelope<ItemChanged> envelope) {
            calls.Lines.Add("both:item");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }

        public Task<Result<Unit>> HandleAsync(IEnvelope<CardChanged> envelope) {
            calls.Lines.Add("both:card");
            return Task.FromResult<Result<Unit>>(Unit.Value);
        }
    }

    public sealed class Audit<TEvent>(Calls calls) : IntegrationEventHandler<TEvent> where TEvent : IntegrationEvent {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<TEvent> envelope) {
            calls.Lines.Add($"audit:{typeof(TEvent).Name}");
            return Done;
        }
    }

    public record BaseChanged : IntegrationEvent;

    public sealed record ZoneMoved : BaseChanged;

    public sealed class BaseChangedHandler(Calls calls) : IntegrationEventHandler<BaseChanged> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<BaseChanged> envelope) {
            calls.Lines.Add("base");
            return Done;
        }
    }

    public sealed class ZoneMovedHandler(Calls calls) : IntegrationEventHandler<ZoneMoved> {
        public override Task<Result<Unit>> HandleAsync(IEnvelope<ZoneMoved> envelope) {
            calls.Lines.Add("zone");
            return Done;
        }
    }

    public interface IStamped;

    public sealed record StampedItem : ItemChanged, IStamped;

    public sealed class StampHandler(Calls calls) : ITestHandler<IStamped> {
        public Task<IMessageResult> RunAsync(IEnvelope<IStamped> envelope) {
            calls.Lines.Add("stamp");
            return Task.FromResult<IMessageResult>((Result<Unit>)Unit.Value);
        }
    }

    public abstract record Call;

    public sealed record Call<T> : Call, ICommand<string>;

    public sealed class ListCallHandler<T> : CommandHandler<Call<List<T>>, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Call<List<T>>> envelope) =>
            Task.FromResult<Result<string>>($"list:{typeof(T).Name}");
    }

    public sealed class ExtraHandler<T, TExtra> : ITestHandler<Call<T>> {
        public Task<IMessageResult> RunAsync(IEnvelope<Call<T>> envelope) =>
            Task.FromResult<IMessageResult>((Result<string>)typeof(TExtra).Name);
    }

    public interface IWrap<T> : ICommand<string>;

    public sealed record DoubleWrap : Call, IWrap<int>, IWrap<string>;

    public sealed class WrapHandler<T> : CommandHandler<IWrap<T>, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<IWrap<T>> envelope) =>
            Task.FromResult<Result<string>>(typeof(T).Name);
    }

    public interface IWrapped<T>;

    public sealed record DoubleWrapped : IntegrationEvent, IWrapped<int>, IWrapped<string>;

    public sealed class WrapAudit<T>(Calls calls) : ITestHandler<IWrapped<T>> {
        public Task<IMessageResult> RunAsync(IEnvelope<IWrapped<T>> envelope) {
            calls.Lines.Add($"wrap:{typeof(T).Name}");
            return Task.FromResult<IMessageResult>((Result<Unit>)Unit.Value);
        }
    }

    public sealed class CheckModule : Module;

    [AutoClose]
    public record Check : ICommand<string>;

    public sealed record SubCheck : Check;

    public sealed class CheckHandler : CommandHandler<Check, string> {
        public override Task<Result<string>> HandleAsync(IEnvelope<Check> envelope) =>
            Task.FromResult<Result<string>>("ok");
    }

    public sealed class CheckValidator<T>(Calls calls) : Validator<T> where T : Check {
        public override Error? Validate(IEnvelope<T> envelope) {
            calls.Lines.Add($"valid:{typeof(T).Name}");
            return null;
        }
    }

    public sealed class CheckHook<T>(Calls calls) : AfterCommandHook<T, string> where T : Check {
        public override Task AfterAsync(IEnvelope<T> envelope, Result<string> outcome) {
            calls.Lines.Add($"hook:{typeof(T).Name}");
            return Task.CompletedTask;
        }
    }
}
