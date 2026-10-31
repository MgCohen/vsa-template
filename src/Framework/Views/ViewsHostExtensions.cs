using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace VSA.Views;

// Blazor, interactive server. Use these only when the views run in the host process.
public static class ViewsHostExtensions {
    public static WebApplicationBuilder AddVsaViews(this WebApplicationBuilder builder) {
        builder.Services.AddRazorComponents().AddInteractiveServerComponents();
        return builder;
    }

    public static WebApplication MapVsaViews<TApp>(this WebApplication app) where TApp : IComponent {
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<TApp>().AddInteractiveServerRenderMode();
        return app;
    }
}
