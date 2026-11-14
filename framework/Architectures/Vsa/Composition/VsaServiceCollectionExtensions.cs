using Microsoft.Extensions.DependencyInjection;
using VSA.Kernel;

namespace VSA.Composition;

public static class VsaServiceCollectionExtensions {
    public static IServiceCollection AddVsa(this IServiceCollection services, params string[] args) =>
        AppBuilder.Create(args).ScanFor(typeof(VSA.Module.Module)).Compose(services);

    public static IServiceCollection AddVsa(this IServiceCollection services, string[] args, Action<AppBuilder> configure) {
        var builder = AppBuilder.Create(args).ScanFor(typeof(VSA.Module.Module));
        configure(builder);
        return builder.Compose(services);
    }
}
