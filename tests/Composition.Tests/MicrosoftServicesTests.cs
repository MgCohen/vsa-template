using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace Composition.Tests;

public sealed class MicrosoftServicesTests {
    [Test]
    public void Each_lifetime_maps_to_the_same_Microsoft_lifetime() {
        var collection = new ServiceCollection();
        IVsaServices services = new MicrosoftServices(collection);

        services.AddSingleton<Clock>();
        services.AddScoped<IGreeter, EnglishGreeter>();
        services.AddTransient<Counter>();

        Assert.That(collection.Single(s => s.ServiceType == typeof(Clock)).Lifetime, Is.EqualTo(ServiceLifetime.Singleton));
        Assert.That(collection.Single(s => s.ServiceType == typeof(IGreeter)).Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
        Assert.That(collection.Single(s => s.ServiceType == typeof(Counter)).Lifetime, Is.EqualTo(ServiceLifetime.Transient));
    }

    [Test]
    public void A_factory_gets_a_resolver_of_the_same_scope() {
        var collection = new ServiceCollection();
        IVsaServices services = new MicrosoftServices(collection);
        services.AddScoped<Counter>();
        services.AddScoped(resolver => new CounterHolder(resolver.Require<Counter>()));
        using var provider = collection.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        var holder = scope.ServiceProvider.GetRequiredService<CounterHolder>();

        Assert.That(holder.Counter, Is.SameAs(scope.ServiceProvider.GetRequiredService<Counter>()));
    }

    [Test]
    public void Replace_in_a_module_keeps_the_lifetime_and_removes_the_default() {
        var collection = new ServiceCollection();
        IVsaServices services = new MicrosoftServices(collection);
        services.AddScoped<IGreeter, EnglishGreeter>();

        new ModuleBuilder("Sample", services, new ConfigurationBuilder().Build()).Replace<IGreeter, FrenchGreeter>();

        var greeter = collection.Single(s => s.ServiceType == typeof(IGreeter));
        Assert.That(greeter.ImplementationType, Is.EqualTo(typeof(FrenchGreeter)));
        Assert.That(greeter.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void Replace_with_no_default_is_an_error() {
        var services = new MicrosoftServices(new ServiceCollection());
        var module = new ModuleBuilder("Sample", services, new ConfigurationBuilder().Build());

        var error = Assert.Throws<InvalidOperationException>(() => module.Replace<IGreeter, FrenchGreeter>());

        Assert.That(error.Message, Is.EqualTo("IGreeter has no default registration to replace."));
    }

    public interface IGreeter;

    public sealed class EnglishGreeter : IGreeter;

    public sealed class FrenchGreeter : IGreeter;

    public sealed class Clock;

    public sealed class Counter;

    public sealed class CounterHolder(Counter counter) {
        public Counter Counter => counter;
    }
}
