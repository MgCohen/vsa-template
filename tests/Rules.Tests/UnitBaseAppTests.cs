namespace Rules.Tests;

using Microsoft.Extensions.DependencyInjection;
using Resources;
using Resources.Domain;
using Umpire.Kernels;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Testing.Unit;

public sealed class UnitBaseAppTests : VsaUnitTest {
    protected override IModuleProvider Modules => new FixedModules(new ResourcesModule());

    [Test]
    public void The_app_of_the_test_has_the_services_of_its_modules() =>
        Assert.That(Provider.GetService<Wallet>(), Is.Not.Null);

    [Test]
    public void A_request_with_no_handler_in_the_app_gives_the_error_of_the_local_host() {
        var error = Assert.ThrowsAsync<InvalidOperationException>(() => Handle(new UnhandledRequest()));

        Assert.That(error!.Message, Does.StartWith($"UnitTestHost has no operation for {typeof(UnhandledRequest).FullName}."));
    }

    [Test]
    public void Each_test_starts_its_own_app() =>
        Assert.That(Provider.GetRequiredService<Wallet>().TryChange(new("Mana"), -10, out _), Is.True);

    [Test]
    public void Each_test_starts_its_own_app_again() =>
        Assert.That(Provider.GetRequiredService<Wallet>().TryChange(new("Mana"), -10, out _), Is.True);
}

public sealed class UnitBaseReplaceTests : VsaUnitTest {
    readonly Wallet wallet = new();

    protected override IModuleProvider Modules => new FixedModules(new ResourcesModule());

    protected override void Configure(IServiceCollection services) => services.AddSingleton(wallet);

    [Test]
    public void A_service_of_the_test_replaces_the_service_of_a_module() {
        Assert.That(Provider.GetRequiredService<Wallet>(), Is.SameAs(wallet));
        Assert.That(Provider.GetServices<Wallet>().Count(), Is.EqualTo(1));
    }
}

public sealed record UnhandledRequest : IRequest<UnhandledResponse>;

public sealed record UnhandledResponse : IResponse;
