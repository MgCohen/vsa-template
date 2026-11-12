using ArchitectureViewer.Domain;
using ArchitectureViewer.Domain.Shapes;
using ArchitectureViewer.Infrastructure;
using ArchitectureViewer.Infrastructure.Elk;
using Microsoft.Extensions.DependencyInjection;
using VSA.Module;

namespace ArchitectureViewer;

public sealed class ArchitectureViewerModule : Module
{
    public override void Configure(ModuleBuilder module)
    {
        module.Services.AddSingleton(_ => DataFolder.Read(AppContext.BaseDirectory));
        module.Services.AddElkLayout();
        module.Services.AddScoped<IClipboard, BrowserClipboard>();
        foreach (Type shape in typeof(INodeShape).Assembly.GetTypes().Where(IsShape))
        {
            module.Services.AddSingleton(typeof(INodeShape), shape);
        }
    }

    private static bool IsShape(Type type)
    {
        return type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(INodeShape));
    }
}
