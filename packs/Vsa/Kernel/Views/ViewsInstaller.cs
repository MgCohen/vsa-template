namespace Umpire.Vsa.Views;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

static class ViewsInstaller {
    internal static Type? FindRoot(IReadOnlyList<Type> roots) =>
        roots switch {
            [] => null,
            [var root] => root,
            _ => throw new InvalidOperationException(
                $"The app has {roots.Count} views roots: {string.Join(", ", roots.Select(t => t.FullName))}. "
                + "Keep one component that inherits ViewsRoot."),
        };

    internal static void Install(IServiceCollection services) =>
        services.AddRazorComponents().AddInteractiveServerComponents();

    internal static void Map(WebApplication app, Type root) {
        app.UseAntiforgery();
        app.MapStaticAssets();
        typeof(ViewsInstaller).GetMethod(nameof(MapRoot), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(root)
            .Invoke(null, [app]);
    }

    static void MapRoot<TRoot>(WebApplication app) where TRoot : ViewsRoot =>
        app.MapRazorComponents<TRoot>().AddInteractiveServerRenderMode();
}
