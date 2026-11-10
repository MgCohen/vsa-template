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
        var types = new ModuleTypes([new MetersModule()]);

        var handlers = types.Implementations(typeof(CommandHandler<,>));
        var validators = types.Implementations(typeof(Validator<>));

        Assert.That(handlers.Select(h => h.Service), Is.EquivalentTo(new[] {
            typeof(CommandHandler<ChangeMeterCommand<Heat>, string>),
            typeof(CommandHandler<ChangeMeterCommand<Charge>, string>),
            typeof(CommandHandler<ChangeMeterCommand<Pressure>, string>),
        }));
        Assert.That(validators.Select(v => v.Service), Is.EquivalentTo(new[] {
            typeof(Validator<ChangeMeterCommand<Heat>>),
            typeof(Validator<ChangeMeterCommand<Charge>>),
            typeof(Validator<ChangeMeterCommand<Pressure>>),
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

    [TestCase(typeof(MarkedStruct), true)]
    [TestCase(typeof(StructOfMarkedInterface), true)]
    [TestCase(typeof(StructOfInterfaceThatExtendsAMarkedInterface), true)]
    [TestCase(typeof(ClassOfMarkedBaseClass), true)]
    [TestCase(typeof(StructOfUnmarkedInterface), false)]
    [TestCase(typeof(UnmarkedStruct), false)]
    [TestCase(typeof(MarkedBaseClass), false)]
    [TestCase(typeof(IMarked), false)]
    public void A_type_is_a_key_when_it_its_base_class_or_an_interface_has_the_marker(Type type, bool isKey) {
        Assert.That(KeyTypes.IsKey(type), Is.EqualTo(isKey));
    }

    [Test]
    public void The_constraint_keys_are_the_candidates_that_meet_each_constraint() {
        var parameter = typeof(OnlyMarkedStructs<>).GetGenericArguments()[0];
        Type[] candidates = [typeof(StructOfMarkedInterface), typeof(MarkedStruct), typeof(ClassOfMarkedBaseClass)];

        var keys = new ConstraintKeys().KeysFor(parameter, candidates);

        Assert.That(keys, Is.EquivalentTo(new[] { typeof(StructOfMarkedInterface) }));
    }

    static VsaBuilder App() => VsaBuilder.Create().UseGenericHost().Modules(new MetersModule());

    [Key]
    public interface IMarked;

    public interface IExtendsMarked : IMarked;

    public interface IUnmarked;

    [Key]
    public readonly record struct MarkedStruct;

    public readonly record struct StructOfMarkedInterface : IMarked;

    public readonly record struct StructOfInterfaceThatExtendsAMarkedInterface : IExtendsMarked;

    public readonly record struct StructOfUnmarkedInterface : IUnmarked;

    public readonly record struct UnmarkedStruct;

    [Key]
    public abstract class MarkedBaseClass;

    public sealed class ClassOfMarkedBaseClass : MarkedBaseClass;

    sealed class OnlyHeat : IKeySource {
        public IEnumerable<Type> KeysFor(Type genericParameter, IReadOnlyList<Type> candidates) =>
            candidates.Where(c => c == typeof(Heat));
    }

    sealed class OnlyMarkedStructs<T> where T : struct, IMarked;
}
