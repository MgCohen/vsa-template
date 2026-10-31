using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Events;
using VSA.Handlers.Events;
using VSA.Handlers.Requests;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    // Registers the defaults, then the handlers and presenters of each module, then calls Configure.
    // The host finds the projects of a module from its name: <Name>.Handlers.Requests, and so on.
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        var services = builder.Services;
        var requests = new RequestCatalog();
        services.AddSingleton(requests);
        services.AddSingleton<IBus, InProcessBus>();
        services.AddScoped<IIntegrationEventPublisher, InProcessEventPublisher>();
        foreach (var module in modules) {
            foreach (var handler in Concrete(module, "Handlers.Requests")) {
                var handles = BaseOf(handler, typeof(RequestHandler<,>));
                services.AddScoped(handles, handler);
                requests.Add(handles.GetGenericArguments()[0], handles.GetGenericArguments()[1]);
            }
            foreach (var handler in Concrete(module, "Handlers.Events")) {
                services.AddScoped(BaseOf(handler, typeof(IntegrationEventHandler<>)), handler);
            }
            foreach (var presenter in Concrete(module, "Presenters").Where(t => t.IsSubclassOf(typeof(Presenter)))) {
                services.AddScoped(presenter);
            }
            module.Configure(new VSA.Module.ModuleBuilder(module.Name, services, builder.Configuration));
        }
        return builder;
    }

    // Maps one HTTP endpoint for each request, from its [HttpRoute].
    public static WebApplication MapVsaRequests(this WebApplication app) {
        var map = typeof(RequestEndpoints).GetMethod(nameof(RequestEndpoints.MapRequest))!;
        foreach (var (request, response) in app.Services.GetRequiredService<RequestCatalog>().All) {
            map.MakeGenericMethod(request, response).Invoke(null, [app]);
        }
        return app;
    }

    static IEnumerable<Type> Concrete(VSA.Module.Module module, string layer) =>
        Assembly.Load($"{module.Name}.{layer}").GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

    static Type BaseOf(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        throw new InvalidOperationException($"{type.FullName} must inherit {openBase.Name}.");
    }
}
