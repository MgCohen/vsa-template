namespace Spike.Pack;

using Microsoft.Extensions.DependencyInjection;
using Spike.Kernels;

public sealed class PackKernel : Kernel {
    protected override void Configure(IServiceCollection services, AppTypes types) {
        foreach (var handler in types.Implementations(typeof(RequestHandler<,>))) {
            services.AddScoped(handler.Service, handler.Type);
            services.AddSingleton(RequestOperations.Create(handler.Service));
        }
    }
}
