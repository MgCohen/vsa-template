namespace Umpire.Vsa.Testing.Unit;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

sealed class UnitTestHost : LocalHost {
    public IServiceCollection TestServices { get; init; } = new ServiceCollection();

    protected override void Configure(IServiceCollection services, AppTypes types) {
        base.Configure(services, types);
        var replaced = TestServices.Select(descriptor => descriptor.ServiceType).Distinct().ToList();
        for (var i = services.Count - 1; i >= 0; i--) {
            if (replaced.Any(service => Replaces(service, services[i].ServiceType))) {
                services.RemoveAt(i);
            }
        }
        foreach (var descriptor in TestServices) {
            services.Add(descriptor);
        }
    }

    static bool Replaces(Type test, Type app) =>
        app == test
        || (test.IsGenericTypeDefinition && app.IsConstructedGenericType && app.GetGenericTypeDefinition() == test);
}
