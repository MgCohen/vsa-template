using ArchitectureViewer.Domain.Layouts;
using Microsoft.Extensions.DependencyInjection;

namespace ArchitectureViewer.Infrastructure.Elk;

public static class ElkSetup
{
    public const long MaximumResultSize = 1024 * 1024;

    public static IServiceCollection AddElkLayout(this IServiceCollection services)
    {
        services.AddScoped<ILayoutEngine, ElkLayout>();
        services.AddServerSideBlazor().AddHubOptions(o => o.MaximumReceiveMessageSize = MaximumResultSize);
        return services;
    }
}
