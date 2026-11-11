using Microsoft.Extensions.DependencyInjection;
using VSA.Composition;

namespace Composition.Tests;

public sealed class FactoryTests {
    public sealed record Greeting(string Text);

    public sealed class Name {
        public string Value { get; set; } = "Ada";
    }

    public sealed class Punctuation {
        public string Mark { get; } = "!";
    }

    public sealed class GreetingFactory(Name name) : IFactory<Greeting>, IFactory<string, Greeting> {
        public Greeting Create() => new($"Hello {name.Value}");

        public Greeting Create(string greeting) => new($"{greeting} {name.Value}");
    }

    public sealed class NoFactory;

    [Test]
    public void A_custom_factory_gets_its_services_from_its_constructor() {
        using var provider = new ServiceCollection().AddSingleton<Name>().AddFactory<GreetingFactory>().BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IFactory<Greeting>>().Create(), Is.EqualTo(new Greeting("Hello Ada")));
    }

    [Test]
    public void A_custom_factory_is_registered_as_each_factory_interface_that_it_implements() {
        using var provider = new ServiceCollection().AddSingleton<Name>().AddFactory<GreetingFactory>().BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IFactory<string, Greeting>>().Create("Hi"), Is.EqualTo(new Greeting("Hi Ada")));
    }

    [Test]
    public void A_type_with_no_factory_interface_is_not_a_factory() {
        var error = Assert.Throws<ArgumentException>(() => new ServiceCollection().AddFactory<NoFactory>());

        Assert.That(error!.Message, Does.Contain("implements no factory interface"));
    }

    [Test]
    public void A_simple_factory_gives_one_service_to_the_function() {
        using var provider = new ServiceCollection().AddSingleton<Name>()
            .AddFactory<Greeting, Name>(name => new Greeting($"Hello {name.Value}")).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IFactory<Greeting>>().Create(), Is.EqualTo(new Greeting("Hello Ada")));
    }

    [Test]
    public void A_simple_factory_gives_two_services_to_the_function() {
        using var provider = new ServiceCollection().AddSingleton<Name>().AddSingleton<Punctuation>()
            .AddFactory<Greeting, Name, Punctuation>((name, punctuation) => new Greeting($"Hello {name.Value}{punctuation.Mark}"))
            .BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IFactory<Greeting>>().Create(), Is.EqualTo(new Greeting("Hello Ada!")));
    }

    [Test]
    public void A_simple_factory_resolves_the_service_on_each_create() {
        using var provider = new ServiceCollection().AddTransient<Name>()
            .AddFactory<Greeting, Name>(name => new Greeting($"Hello {name.GetHashCode()}")).BuildServiceProvider();
        var factory = provider.GetRequiredService<IFactory<Greeting>>();
        var first = factory.Create();

        Assert.That(factory.Create(), Is.Not.EqualTo(first));
    }

    [Test]
    public void A_simple_factory_resolves_scoped_services_from_the_scope_that_resolved_it() {
        using var provider = new ServiceCollection().AddScoped<Name>()
            .AddFactory<Greeting, Name>(name => new Greeting($"Hello {name.Value}"))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<Name>().Value = "Grace";

        var greeting = scope.ServiceProvider.GetRequiredService<IFactory<Greeting>>().Create();

        Assert.That(greeting, Is.EqualTo(new Greeting("Hello Grace")));
    }
}
