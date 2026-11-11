using Microsoft.Extensions.DependencyInjection;

namespace VSA.Composition;

public static class VsaServiceCollectionExtensions {
    public static IServiceCollection AddVsa(this IServiceCollection services, params string[] args) =>
        VsaBuilder.Create(args).Compose(services);

    public static IServiceCollection AddVsa(this IServiceCollection services, string[] args, Action<VsaBuilder> configure) {
        var builder = VsaBuilder.Create(args);
        configure(builder);
        return builder.Compose(services);
    }
}
