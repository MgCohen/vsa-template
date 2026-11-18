namespace Umpire.Hosts.Web;

using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Umpire.Kernels;

static class Views {
    internal static Type? RootOf(AppTypes types) => FindRoot(types.ConcreteTypesOf(typeof(ViewsRoot)));

    internal static Type? FindRoot(IReadOnlyList<Type> roots) =>
        roots switch {
            [] => null,
            [var root] => root,
            _ => throw new InvalidOperationException(
                $"The app has {roots.Count} views roots: {string.Join(", ", roots.Select(t => t.FullName))}. "
                + "Keep one component that inherits ViewsRoot."),
        };

    internal static void Map(WebApplication app, Type root) {
        app.UseAntiforgery();
        app.MapStaticAssets();
        typeof(Views).GetMethod(nameof(MapRoot), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(root)
            .Invoke(null, [app]);
    }

    static void MapRoot<TRoot>(WebApplication app) where TRoot : ViewsRoot =>
        app.MapRazorComponents<TRoot>().AddInteractiveServerRenderMode();
}
