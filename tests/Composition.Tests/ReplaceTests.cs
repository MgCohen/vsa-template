namespace Composition.Tests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

public sealed class ReplaceTests {
    [Test]
    public void Replace_keeps_the_lifetime_and_removes_the_default() {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter, EnglishGreeter>();

        services.Replace<IGreeter, FrenchGreeter>();

        var greeter = services.Single(s => s.ServiceType == typeof(IGreeter));
        Assert.That(greeter.ImplementationType, Is.EqualTo(typeof(FrenchGreeter)));
        Assert.That(greeter.Lifetime, Is.EqualTo(ServiceLifetime.Scoped));
    }

    [Test]
    public void Replace_with_no_default_is_an_error() {
        var services = new ServiceCollection();

        var error = Assert.Throws<InvalidOperationException>(() => services.Replace<IGreeter, FrenchGreeter>());

        Assert.That(error.Message, Is.EqualTo("IGreeter has no default registration to replace."));
    }

    public interface IGreeter;

    public sealed class EnglishGreeter : IGreeter;

    public sealed class FrenchGreeter : IGreeter;
}
