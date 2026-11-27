namespace Spike.Pack;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Spike.Core;

public sealed class PackKernel : Kernel {
    protected override void Configure(IServiceCollection services, Assembly app) {
        var handlers = new Dictionary<Type, HandlerRoute>();
        var requests = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in app.GetTypes().Where(t => !t.IsAbstract && HandlerBase(t) is not null)) {
            var service = HandlerBase(type)!;
            var arguments = service.GetGenericArguments();
            if (handlers.ContainsKey(arguments[0])) {
                throw new InvalidOperationException($"{arguments[0].Name} has two handlers.");
            }
            if (requests.ContainsKey(NameOf(arguments[0]))) {
                throw new InvalidOperationException($"Two requests have the name {NameOf(arguments[0])}.");
            }
            services.AddScoped(service, type);
            var call = (IHandlerCall)Activator.CreateInstance(typeof(RequestCall<,>).MakeGenericType(arguments))!;
            handlers[arguments[0]] = new HandlerRoute(service, call);
            requests[NameOf(arguments[0])] = arguments[0];
        }
        services.AddSingleton<IPipeline>(provider => new Pipeline(handlers, provider.GetServices<StepEntry>(), provider.GetServices<ValidatorEntry>()));

        services.TryAddSingleton<IConstructorFactory, CompiledConstructors>();
        services.AddSingleton<IOperationConverter>(provider => new RequestConverter(requests, provider.GetRequiredService<IConstructorFactory>()));
    }

    static Type? HandlerBase(Type type) {
        for (var current = type.BaseType; current is not null; current = current.BaseType) {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(RequestHandler<,>)) {
                return current;
            }
        }
        return null;
    }

    static string NameOf(Type request) =>
        request.Name.EndsWith("Request", StringComparison.Ordinal)
            ? request.Name.Substring(0, request.Name.Length - "Request".Length)
            : request.Name;
}
