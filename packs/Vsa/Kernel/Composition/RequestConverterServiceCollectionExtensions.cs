namespace Umpire.Vsa.Composition;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

public static class RequestConverterServiceCollectionExtensions {
    public static IServiceCollection AddRequestConverter(this IServiceCollection services, AppTypes types) =>
        services.AddSingleton<IOperationConverter>(RequestConverter.Of(types));
}
