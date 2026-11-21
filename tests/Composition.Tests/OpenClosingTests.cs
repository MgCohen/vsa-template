namespace Composition.Tests;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Till;
using Till.Domain;
using Till.Domain.Shared;
using Till.Slices.Change;
using Till.Slices.Pick;
using Till.Slices.Put;
using Till.Slices.Swap;
using Umpire.Kernels;
using Umpire.Vsa.Contracts.Commands;
using Umpire.Vsa.Contracts.Events;
using Relay;
using Relay.Slices.Pass;
using Unbound;
using Unclosed;

public sealed class OpenClosingTests {
    [Test]
    public async Task An_open_command_handler_of_a_nested_message_handles_the_auto_close_type() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();

        var put = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PutCommand<Score>(3), default);

        Assert.That(put.Value, Is.EqualTo(3));
    }

    [Test]
    public async Task An_open_validator_of_a_nested_message_checks_the_auto_close_type() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();

        var put = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PutCommand<Score>(-1), default);

        Assert.That(put.Error, Is.EqualTo(TillErrors.Negative));
    }

    [Test]
    public async Task An_open_event_handler_of_a_nested_event_gets_the_event_of_the_auto_close_type() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();

        await scope.ServiceProvider.GetRequiredService<IIntegrationEventPublisher>()
            .PublishAsync(new ChangedEvent<Score>(2), default);

        Assert.That(scope.ServiceProvider.GetRequiredService<TillLog>().Lines, Is.EqualTo(new[] { "changed:Score:2" }));
    }

    [Test]
    public void The_constructor_walk_closes_a_module_open_registration_over_a_struct_with_no_attribute() {
        using var host = App().Build();

        var register = host.Services.GetRequiredService<Register>();

        Assert.That(register.Coins, Is.TypeOf<Stock<Coin>>());
        Assert.That(register.Gems.Select(g => g.GetType()), Is.EqualTo(new[] { typeof(Stock<Gem>) }));
    }

    [Test]
    public void The_constructor_walk_closes_an_open_auto_register_class_with_two_type_parameters() {
        using var host = App().Build();
        using var scope = host.Services.CreateScope();

        var register = host.Services.GetRequiredService<Register>();
        var scores = scope.ServiceProvider.GetRequiredService<IExchange<Score, Score>>();

        Assert.That(register.Exchange.Rate, Is.EqualTo("Coin->Score"));
        Assert.That(scores.Rate, Is.EqualTo("Score->Score"));
    }

    [Test]
    public void A_nested_module_registration_gives_the_implementation_closed_over_the_inner_type() {
        using var host = App().Build();

        var entry = host.Services.GetRequiredService<IEntry<PutCommand<Score>>>();

        Assert.That(entry.Entry, Is.EqualTo(typeof(Entry<Score>)));
    }

    [Test]
    public void A_list_of_an_open_and_a_closed_registration_of_one_type_has_one_item() {
        using var host = App().Build();

        var shelves = host.Services.GetServices<IShelf<Coin>>();

        Assert.That(shelves.Count(), Is.EqualTo(1));
    }

    [Test]
    public void A_module_registration_of_a_scanned_class_does_not_add_a_second_item() {
        using var host = App().Build();
        using var scope = host.Services.CreateScope();

        var counters = scope.ServiceProvider.GetServices<ICounter<Score>>();

        Assert.That(counters.Select(c => c.GetType()), Is.EqualTo(new[] { typeof(Counter<Score>) }));
    }

    [Test]
    public void The_closed_descriptors_replace_the_open_descriptor_with_the_same_lifetime() {
        var services = Composed(new TillModule());

        Assert.That(services.Any(d => d.ServiceType == typeof(IStock<>)), Is.False);
        Assert.That(services.Single(d => d.ServiceType == typeof(IStock<Coin>)).Lifetime,
            Is.EqualTo(ServiceLifetime.Singleton));
    }

    [Test]
    public void An_open_auto_register_class_with_no_type_argument_stops_the_start() {
        var start = () => new TestApp(new UnclosedModule()).Build();

        Assert.That(start, Throws.InvalidOperationException.With.Message.EqualTo(
            "Unclosed.Domain.Rack<T> has no type argument. "
            + "Mark a type that meets the constraints of T with [AutoClose], or remove [AutoRegister]."));
    }

    [Test]
    public async Task The_dispatch_closes_an_open_command_handler_for_a_struct_with_no_attribute() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();

        var put = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new PutCommand<Coin>(5), default);

        Assert.That(put.Value, Is.EqualTo(5));
    }

    [Test]
    public async Task The_dispatch_closes_an_open_handler_with_two_type_parameters() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();

        var swap = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new SwapCommand<Coin, Gem>(), default);

        Assert.That(swap.Value, Is.EqualTo("Coin->Gem"));
    }

    [Test]
    public async Task An_open_handler_with_each_type_parameter_in_its_message_needs_no_auto_close_type() {
        using var host = new TestApp(new RelayModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();

        var relay = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new RelayCommand<int>(4), default);

        Assert.That(relay.Value, Is.EqualTo("Int32 4"));
    }

    [Test]
    public async Task The_start_closes_an_open_handler_for_each_declared_message_of_its_shape() {
        using var host = new TestApp(new RelayModule()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var open = typeof(RelayModule).Assembly.GetTypes().Single(t => t.Name == "RelayHandler`1");

        var handler = scope.ServiceProvider.GetService(open.MakeGenericType(typeof(string)));
        var relay = await scope.ServiceProvider.GetRequiredService<ICommandSender>()
            .SendAsync(new NameRelay("Ada"), default);

        Assert.That(handler, Is.Not.Null);
        Assert.That(relay.Value, Is.EqualTo("String Ada"));
    }

    [Test]
    public async Task A_failed_dispatch_close_names_the_handler_the_message_and_the_constraint() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var send = async () => await sender.SendAsync(new PickCommand<int>(), default);

        Assert.That(send, Throws.InvalidOperationException.With.Message.Contains(
            "Till.Slices.Pick.PickHandler<T> cannot close for PickCommand`1: "
            + "Int32 does not meet the constraints of T: struct, IAmount."));
    }

    [Test]
    public void An_open_handler_with_a_type_parameter_that_is_not_in_its_message_stops_the_start() {
        var start = () => new TestApp(new UnboundModule()).Build();

        Assert.That(start, Throws.InvalidOperationException.With.Message.EqualTo(
            "Unbound.Slices.Mark.MarkHandler<T, TLabel> has no type argument. "
            + "Mark a type that meets the constraints of T, TLabel with [AutoClose], or remove [AutoRegister]."));
    }

    [Test]
    public void Unify_solves_the_type_parameter_of_a_nested_service() {
        var closed = OpenGenerics.Unify(typeof(Entry<>), typeof(IEntry<PutCommand<Coin>>));

        Assert.That(closed, Is.EqualTo(typeof(Entry<Coin>)));
    }

    [Test]
    public void Unify_solves_two_type_parameters() {
        var closed = OpenGenerics.Unify(typeof(Exchange<,>), typeof(IExchange<Gem, Coin>));

        Assert.That(closed, Is.EqualTo(typeof(Exchange<Gem, Coin>)));
    }

    [Test]
    public void Unify_gives_no_type_when_an_argument_does_not_meet_a_constraint() {
        var closed = OpenGenerics.Unify(typeof(Counter<>), typeof(ICounter<int>));

        Assert.That(closed, Is.Null);
    }

    static TestApp App() => new(new TillModule());

    static ServiceCollection Composed(Module module) {
        var services = new ServiceCollection();
        using var host = new TestHost();
        host.Compose(services, new ConfigurationBuilder().Build(),
            new Kernel(), new AppTypes(new FixedModules(module)));
        return services;
    }
}
