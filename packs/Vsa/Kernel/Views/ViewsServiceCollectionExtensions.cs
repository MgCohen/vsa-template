namespace Umpire.Vsa.Views;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernel;

public static class ViewsServiceCollectionExtensions {
    public static IServiceCollection AddViews(this IServiceCollection services, AppTypes types) {
        ArgumentNullException.ThrowIfNull(types);
        if (RootOf(types) is not null) {
            services.AddRazorComponents().AddInteractiveServerComponents();
        }
        return services;
    }

    public static WebApplication MapViews(this WebApplication app) {
        ArgumentNullException.ThrowIfNull(app);
        if (RootOf(app.Services.GetRequiredService<AppTypes>()) is not { } root) {
            return app;
        }
        app.UseAntiforgery();
        app.MapStaticAssets();
        typeof(ViewsServiceCollectionExtensions).GetMethod(nameof(MapRoot), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(root)
            .Invoke(null, [app]);
        return app;
    }

    static Type? RootOf(AppTypes types) =>
        types.ConcreteTypesOf(typeof(ViewsRoot)) switch {
            [] => null,
            [var root] => root,
            var roots => throw new InvalidOperationException(
                $"The app has {roots.Count} views roots: {string.Join(", ", roots.Select(t => t.FullName))}. "
                + "Keep one component that inherits ViewsRoot."),
        };

    static void MapRoot<TRoot>(WebApplication app) where TRoot : ViewsRoot =>
        app.MapRazorComponents<TRoot>().AddInteractiveServerRenderMode();
}
