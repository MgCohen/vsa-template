using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

namespace Composition.Tests;

public sealed class FactoryTests {
    public sealed record Greeting(string Text);

    public sealed class Name {
        public string Value { get; set; } = "Ada";
    }

    [Test]
    public void A_simple_factory_gives_one_service_to_the_function() {
        using var provider = new ServiceCollection().AddSingleton<Name>()
            .AddFactory<Greeting, Name>(name => new Greeting($"Hello {name.Value}")).BuildServiceProvider();

        Assert.That(provider.GetRequiredService<IFactory<Greeting>>().Create(), Is.EqualTo(new Greeting("Hello Ada")));
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
