using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts.Commands;
using VSA.Contracts.Events;
using VSA.Handlers.Commands;
using VSA.Handlers.Events;
using VSA.Module;
#if NET
using VSA.Presenters;
#endif

namespace VSA.Composition;

public static class VsaServiceExtensions {
    public static IServiceCollection AddVsa(this IServiceCollection services, IConfiguration configuration) =>
        services.AddVsa(configuration, DiscoveredModules());

    public static IServiceCollection AddVsa(
        this IServiceCollection services, IConfiguration configuration, params VSA.Module.Module[] modules) =>
        services.AddVsaCore().AddVsaModules(configuration, modules);

    public static IServiceCollection AddVsaCore(this IServiceCollection services) {
        services.AddVsaEvents();
        services.AddVsaCommands();
#if NET
        services.AddVsaLivePresenters();
#endif
        return services;
    }

    public static IServiceCollection AddVsaModules(this IServiceCollection services, IConfiguration configuration) =>
        services.AddVsaModules(configuration, DiscoveredModules());

    public static IServiceCollection AddVsaModules(
        this IServiceCollection services, IConfiguration configuration, params VSA.Module.Module[] modules) {
        RequireCore(services);
        var commandHandlers = new Dictionary<Type, Type>();
        foreach (var module in modules) {
            var root = ModuleRoot.Of(module);
            AddEventHandlers(services, root);
            AddCommandHandlers(services, root, commandHandlers);
#if NET
            AddPresenters(services, root);
#endif
            module.Configure(new ModuleBuilder(module.Name, services, configuration));
        }
        return services;
    }

    public static VSA.Module.Module[] DiscoveredModules() =>
        [.. ModuleDiscovery.CreateModules(ModuleDiscovery.FindRoots())];

    static void RequireCore(IServiceCollection services) {
        Type[] coreServices = [typeof(IIntegrationEventPublisher), typeof(ICommandSender)];
        var missing = coreServices.Where(core => !services.Any(s => s.ServiceType == core)).ToArray();
        if (missing.Length > 0) {
            throw new InvalidOperationException(
                $"AddVsaModules needs {string.Join(" and ", missing.Select(m => m.Name))}. "
                + "Call AddVsaCore before AddVsaModules, or call AddVsa.");
        }
    }

    static void AddEventHandlers(IServiceCollection services, ModuleRoot root) {
        foreach (var handler in ConcreteTypes(root, "Handlers.Events")) {
            if (ClosedBase(handler, typeof(IntegrationEventHandler<>)) is { } handledEvent) {
                services.AddScoped(handledEvent, handler);
            }
        }
    }

    static void AddCommandHandlers(
        IServiceCollection services, ModuleRoot root, Dictionary<Type, Type> handlerOfCommand) {
        foreach (var handler in ConcreteTypes(root, "Handlers.Commands")) {
            if (ClosedBase(handler, typeof(CommandHandler<,>)) is not { } service) {
                continue;
            }
            if (handlerOfCommand.TryGetValue(service, out var other)) {
                throw new InvalidOperationException(
                    $"Command {service.GetGenericArguments()[0].FullName} has two handlers: "
                    + $"{other.FullName} and {handler.FullName}. A command has one handler.");
            }
            handlerOfCommand[service] = handler;
            services.AddScoped(service, handler);
        }
    }

#if NET
    static void AddPresenters(IServiceCollection services, ModuleRoot root) {
        foreach (var presenter in ConcreteTypes(root, "Presenters").Where(t => t.IsSubclassOf(typeof(Presenter)))) {
            services.AddVsaPresenter(presenter);
        }
    }
#endif

    static IEnumerable<Type> ConcreteTypes(ModuleRoot root, string layer) =>
        (root.LayerAssembly(layer)?.GetTypes() ?? [])
            .Where(t => t is { IsClass: true, IsAbstract: false } && !t.IsDefined(typeof(CompilerGeneratedAttribute)));

    static Type? ClosedBase(Type type, Type openBase) {
        for (var b = type.BaseType; b is not null; b = b.BaseType) {
            if (b.IsGenericType && b.GetGenericTypeDefinition() == openBase) {
                return b;
            }
        }
        return null;
    }
}
