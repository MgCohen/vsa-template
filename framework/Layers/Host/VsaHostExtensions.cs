using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Presenters;

namespace VSA.Host;

public static class VsaHostExtensions {
    // Finds the modules of the app (ModuleDiscovery), then registers them as AddVsa(modules) does.
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder) =>
        builder.AddVsa([.. VSA.Module.ModuleDiscovery.CreateModules(VSA.Module.ModuleDiscovery.FindRoots())]);

    // Registers the defaults, then the handlers and presenters of each module, then calls Configure.
    // The host finds the projects of a module from the layers that its root recorded at build time.
    public static WebApplicationBuilder AddVsa(this WebApplicationBuilder builder, params VSA.Module.Module[] modules) {
        var services = builder.Services;
        services.AddVsaBus(LoopbackAddress);
        services.AddVsaEvents();
        services.AddVsaCommands();
        // The live presenters get each event after its handlers (IPresenterEvent).
        services.AddVsaLivePresenters();
        // FastEndpoints needs at least one assembly when auto-discovery is off.
        // An app with no Handlers.Requests layer does not register FastEndpoints and maps no endpoints.
        Assembly[] requests = [.. modules.Select(module => Load(module, "Handlers.Requests")).OfType<Assembly>()];
        if (requests.Length > 0) {
            services.AddFastEndpoints(options => {
                options.DisableAutoDiscovery = true;
                options.Assemblies = requests;
            });
            services.AddSingleton<RequestEndpoints>();
        }
        // A command has one handler in the app. A second handler of the same command stops start-up.
        var commandHandlers = new Dictionary<Type, Type>();
        foreach (var module in modules) {
            // Only the event handlers: a Handlers.Events project also has the helper types of its slices.
            foreach (var handler in Concrete(module, "Handlers.Events")) {
                if (BaseOf(handler, typeof(IntegrationEventHandler<>)) is { } handled) {
                    services.AddScoped(handled, handler);
                }
            }
            foreach (var handler in Concrete(module, "Handlers.Commands")) {
                // A slice can have helper classes: only a CommandHandler is registered.
                // The full name: FastEndpoints also has a CommandHandler type.
                if (BaseOf(handler, typeof(VSA.Handlers.Commands.CommandHandler<,>)) is not { } service) {
                    continue;
                }
                if (commandHandlers.TryGetValue(service, out var other)) {
                    throw new InvalidOperationException(
                        $"Command {service.GetGenericArguments()[0].FullName} has two handlers: {other.FullName} and {handler.FullName}. A command has one handler.");
                }
                commandHandlers[service] = handler;
                services.AddScoped(service, handler);
            }
            foreach (var presenter in Concrete(module, "Presenters").Where(t => t.IsSubclassOf(typeof(Presenter)))) {
                services.AddVsaPresenter(presenter);
            }
            module.Configure(new VSA.Module.ModuleBuilder(module.Name, services, builder.Configuration));
        }
        return builder;
    }

    // Maps one HTTP endpoint for each request handler. With no request handlers, it does nothing.
    public static WebApplication MapVsaRequests(this WebApplication app) {
        if (app.Services.GetService<RequestEndpoints>() is not null) {
            app.UseFastEndpoints();
        }
        return app;
    }

    // Registered only when AddVsa registers FastEndpoints.
    sealed class RequestEndpoints { }

    // VSA.Sdk records the layer projects of the module root (ModuleRoot.Layers).
    // A layer that the module does not have gives null. A recorded layer that does not load is an error.
    static Assembly? Load(VSA.Module.Module module, string layer) {
        var name = $"{module.Name}.{layer}";
        return VSA.Module.ModuleRoot.Of(module).Layers.Contains(name) ? Assembly.Load(name) : null;
    }

    static IEnumerable<Type> Concrete(VSA.Module.Module module, string layer) =>
        (Load(module, layer)?.GetTypes() ?? [])
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)));

    // The closed base of type that is openBase, or null.
    static Type? BaseOf(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        return null;
    }

    // The first address of this server, with a wildcard host replaced by localhost.
    static Uri LoopbackAddress(IServiceProvider provider) {
        var addresses = provider.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var address = addresses.FirstOrDefault(a => a.StartsWith("http://", StringComparison.Ordinal)) ?? addresses.FirstOrDefault()
            ?? throw new InvalidOperationException("The server has no address for the bus.");
        foreach (var wildcard in new[] { "://+", "://*", "://[::]", "://0.0.0.0" }) {
            address = address.Replace(wildcard, "://localhost", StringComparison.Ordinal);
        }
        return new Uri(address);
    }
}
