namespace Spike.Pack;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spike.Core;

public abstract class Plugin : Core.Module;

public sealed class PackKernel : Kernel {
    public override void Configure(IServiceCollection services, Assembly app) {
        var handlers = new Dictionary<Type, HandlerCall>();
        var requests = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var handler in app.GetTypes().Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b
            && b.GetGenericTypeDefinition() == typeof(RequestHandler<,>))) {
            var request = handler.BaseType!.GetGenericArguments()[0];
            services.AddScoped(handler);
            handlers[request] = (scope, message, ct) => ((IRequestHandler)scope.GetRequiredService(handler)).Handle(message, ct);
            requests[request.Name.Replace("Request", "", StringComparison.Ordinal)] = request;
        }
        services.AddSingleton<IPipeline>(provider => new Pipeline(handlers, provider.GetServices<PipelineEntry>()));
        services.AddSingleton<IOperationConverter>(new RequestConverter(requests));
    }
}
