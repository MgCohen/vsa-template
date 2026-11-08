using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace Composition.Tests;

public sealed class ModuleBuilderTests {
    [Test]
    public void Replace_in_a_module_keeps_the_lifetime_and_removes_the_default() {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter, EnglishGreeter>();

        Builder(services).Replace<IGreeter, FrenchGreeter>();

        var greeter = services.Single(s => s.ServiceType == typeof(IGreeter));
        Assert.That(greeter.ImplementationType, Is.EqualTo(typeof(FrenchGreeter)));
        Assert.That(greeter.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void Replace_with_no_default_is_an_error() {
        var module = Builder(new ServiceCollection());

        var error = Assert.Throws<InvalidOperationException>(() => module.Replace<IGreeter, FrenchGreeter>());

        Assert.That(error.Message, Is.EqualTo("IGreeter has no default registration to replace."));
    }

    static ModuleBuilder Builder(IServiceCollection services) =>
        new("Sample", services, new ConfigurationBuilder().Build());

    public interface IGreeter;

    public sealed class EnglishGreeter : IGreeter;

    public sealed class FrenchGreeter : IGreeter;
}
