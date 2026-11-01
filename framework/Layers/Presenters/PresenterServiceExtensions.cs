using Microsoft.Extensions.DependencyInjection;

namespace VSA.Presenters;

public static class PresenterServiceExtensions {
    // Registers IBus as HttpBus. The base address is the server that maps the request handlers.
    public static IServiceCollection AddVsaBus(this IServiceCollection services, Func<IServiceProvider, Uri> baseAddress) {
        services.AddHttpClient<IBus, HttpBus>((provider, client) => client.BaseAddress = baseAddress(provider));
        return services;
    }
}
