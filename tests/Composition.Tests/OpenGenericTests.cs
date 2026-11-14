using Meters;
using Meters.Domain.Shared;
using Meters.Slices.ChangeMeter;
using Microsoft.Extensions.DependencyInjection;
using VSA.Analyzers.Markers;
using VSA.Composition;
using VSA.Contracts.Commands;
using VSA.Handlers;
using VSA.Handlers.Commands;
using VSA.Module;
using VSA.Kernel;

namespace Composition.Tests;

public sealed class OpenGenericTests {
    [Test]
    public async Task An_open_command_handler_handles_each_key_that_meets_its_constraints() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var heat = await sender.SendAsync(new ChangeMeterCommand<Heat>(2), default);
        var charge = await sender.SendAsync(new ChangeMeterCommand<Charge>(-1), default);
        var pressure = await sender.SendAsync(new ChangeMeterCommand<Pressure>(3), default);

        Assert.That(heat.Value, Is.EqualTo("Heat 2"));
        Assert.That(charge.Value, Is.EqualTo("Charge -1"));
        Assert.That(pressure.Value, Is.EqualTo("Pressure 3"));
    }

    [Test]
    public async Task An_assembly_attribute_gives_a_type_argument_that_cannot_have_the_attribute() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var count = await sender.SendAsync(new ChangeMeterCommand<int>(4), default);

        Assert.That(count.Value, Is.EqualTo("Int32 4"));
    }

    [Test]
    public void A_type_with_no_key_marker_gets_no_closed_handler() {
        using var host = App().Build();
        using var scope = host.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var error = Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(new ChangeMeterCommand<Weight>(1), default));

        Assert.That(error!.Message, Does.Contain("has no handler"));
    }

    [Test]
    public async Task An_open_validator_checks_each_key_that_meets_its_constraints() {
        using var host = App().Build();
        await using var scope = host.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var heat = await sender.SendAsync(new ChangeMeterCommand<Heat>(0), default);
        var pressure = await sender.SendAsync(new ChangeMeterCommand<Pressure>(0), default);

        Assert.That(heat.Error, Is.EqualTo(MeterErrors.NoChange));
        Assert.That(pressure.Error, Is.EqualTo(MeterErrors.NoChange));
    }

    [Test]
    public void The_module_types_hold_one_closed_type_for_each_key_and_no_generic_definition() {
        var types = new AnchorTypes([new MetersModule()]);

        var handlers = types.Implementations(typeof(CommandHandler<,>));
        var validators = types.Implementations(typeof(Validator<>));

        Assert.That(handlers.Select(h => h.Service), Is.EquivalentTo(new[] {
            typeof(CommandHandler<ChangeMeterCommand<Heat>, string>),
            typeof(CommandHandler<ChangeMeterCommand<Charge>, string>),
            typeof(CommandHandler<ChangeMeterCommand<Pressure>, string>),
            typeof(CommandHandler<ChangeMeterCommand<int>, string>),
        }));
        Assert.That(validators.Select(v => v.Service), Is.EquivalentTo(new[] {
            typeof(Validator<ChangeMeterCommand<Heat>>),
            typeof(Validator<ChangeMeterCommand<Charge>>),
            typeof(Validator<ChangeMeterCommand<Pressure>>),
            typeof(Validator<ChangeMeterCommand<int>>),
        }));
        Assert.That(handlers.Concat(validators).Select(h => h.Implementation),
            Has.None.Matches<Type>(t => t.ContainsGenericParameters));
    }

    [Test]
    public async Task A_key_source_of_the_app_replaces_the_constraint_scan() {
        using var host = App().Keys(new OnlyHeat()).Build();
        await using var scope = host.Services.CreateAsyncScope();
        var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();

        var heat = await sender.SendAsync(new ChangeMeterCommand<Heat>(2), default);
        var charge = Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(new ChangeMeterCommand<Charge>(1), default));

        Assert.That(heat.Value, Is.EqualTo("Heat 2"));
        Assert.That(charge!.Message, Does.Contain("has no handler"));
    }

    [Test]
    public void The_services_give_the_module_types_with_the_closed_types() {
        using var host = App().Build();

        var types = host.Services.GetRequiredService<AnchorTypes>();

        Assert.That(types.Implementations(typeof(CommandHandler<,>)).Select(h => h.Service),
            Does.Contain(typeof(CommandHandler<ChangeMeterCommand<Pressure>, string>)));
    }

    [TestCase(typeof(MarkedStruct), true)]
    [TestCase(typeof(StructOfMarkedInterface), true)]
    [TestCase(typeof(StructOfInterfaceThatExtendsAMarkedInterface), true)]
    [TestCase(typeof(ClassOfMarkedBaseClass), true)]
    [TestCase(typeof(StructOfUnmarkedInterface), false)]
    [TestCase(typeof(UnmarkedStruct), false)]
    [TestCase(typeof(MarkedBaseClass), false)]
    [TestCase(typeof(IMarked), false)]
    public void A_type_is_a_type_argument_when_it_its_base_class_or_an_interface_has_the_marker(Type type, bool isKey) {
        Assert.That(MarkedTypes.IsTypeArgument(type), Is.EqualTo(isKey));
    }

    [Test]
    public void The_constraint_keys_are_the_candidates_that_meet_each_constraint() {
        var parameter = typeof(OnlyMarkedStructs<>).GetGenericArguments()[0];
        Type[] candidates = [typeof(StructOfMarkedInterface), typeof(MarkedStruct), typeof(ClassOfMarkedBaseClass)];

        var keys = new ConstraintKeys().KeysFor(parameter, candidates);

        Assert.That(keys, Is.EquivalentTo(new[] { typeof(StructOfMarkedInterface) }));
    }

    [Test]
    public void The_constraint_keys_meet_an_unmanaged_constraint_only_with_unmanaged_structs() {
        var parameter = typeof(OnlyUnmanaged<>).GetGenericArguments()[0];
        Type[] candidates = [typeof(MarkedStruct), typeof(MarkedStructWithText), typeof(MarkedStructWithNumbers)];

        var keys = new ConstraintKeys().KeysFor(parameter, candidates);

        Assert.That(keys, Is.EquivalentTo(new[] { typeof(MarkedStruct), typeof(MarkedStructWithNumbers) }));
    }

    [Test]
    public void An_open_class_with_the_marker_interface_is_closed_and_registered_as_its_marked_interface() {
        using var host = App().Build();
        using var scope = host.Services.CreateScope();

        var rules = scope.ServiceProvider.GetServices<IMeterRule>();

        Assert.That(rules.Select(r => r.Meter), Is.EquivalentTo(new[] { "Heat", "Charge" }));
    }

    [Test]
    public void The_lifetime_of_the_attribute_is_the_lifetime_of_the_registration() {
        using var host = App().Build();
        var cache = Registered("MeterCache`1").MakeGenericType(typeof(Heat));
        using var first = host.Services.CreateScope();
        using var second = host.Services.CreateScope();

        var a = first.ServiceProvider.GetRequiredService(cache);
        var b = second.ServiceProvider.GetRequiredService(cache);

        Assert.That(a, Is.SameAs(b));
    }

    [Test]
    public void One_instance_in_a_scope_serves_the_type_and_its_marked_interface() {
        using var host = App().Build();
        using var scope = host.Services.CreateScope();
        var rule = Registered("MeterRule`1").MakeGenericType(typeof(Heat));

        var byType = scope.ServiceProvider.GetRequiredService(rule);
        var byInterface = scope.ServiceProvider.GetServices<IMeterRule>().Single(r => r.Meter == "Heat");

        Assert.That(byInterface, Is.SameAs(byType));
    }

    [Test]
    public void An_open_class_with_no_marker_is_not_closed_and_not_registered() {
        var types = new AnchorTypes([new MetersModule()]);

        Assert.That(types.Registered().Select(t => t.Name), Has.None.EqualTo("MeterHelper`1"));
    }

    static Type Registered(string name) =>
        new AnchorTypes([new MetersModule()]).Registered().First(t => t.Name == name).GetGenericTypeDefinition();

    static AppBuilder App() => AppBuilder.Create().UseGenericHost().Modules(new MetersModule());

    [TypeArgument]
    public interface IMarked;

    public interface IExtendsMarked : IMarked;

    public interface IUnmarked;

    [TypeArgument]
    public readonly record struct MarkedStruct;

    public readonly record struct StructOfMarkedInterface : IMarked;

    public readonly record struct StructOfInterfaceThatExtendsAMarkedInterface : IExtendsMarked;

    public readonly record struct StructOfUnmarkedInterface : IUnmarked;

    public readonly record struct UnmarkedStruct;

    [TypeArgument]
    public readonly record struct MarkedStructWithText(string Text);

    [TypeArgument]
    public readonly record struct MarkedStructWithNumbers(int Count, Zone Zone, MarkedStruct Inner);

    public enum Zone { Hand }

    [TypeArgument]
    public abstract class MarkedBaseClass;

    public sealed class ClassOfMarkedBaseClass : MarkedBaseClass;

    sealed class OnlyHeat : IKeySource {
        public IEnumerable<Type> KeysFor(Type genericParameter, IReadOnlyList<Type> candidates) =>
            candidates.Where(c => c == typeof(Heat));
    }

    sealed class OnlyMarkedStructs<T> where T : struct, IMarked;

    sealed class OnlyUnmanaged<T> where T : unmanaged;
}
