namespace Spike.Pack;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spike.Core;

public sealed class PackKernel : Kernel {
    protected override void Configure(IServiceCollection services, Assembly app) {
        var handlers = new Dictionary<Type, HandlerRoute>();
        var binders = new Dictionary<string, RequestBinder>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in app.GetTypes().Where(t => !t.IsAbstract && HandlerBase(t) is not null)) {
            var service = HandlerBase(type)!;
            var arguments = service.GetGenericArguments();
            services.AddScoped(service, type);
            var call = (IHandlerCall)Activator.CreateInstance(typeof(RequestCall<,>).MakeGenericType(arguments))!;
            handlers[arguments[0]] = new HandlerRoute(service, call);
            binders[NameOf(arguments[0])] = new RequestBinder(arguments[0]);
        }
        services.AddSingleton<IPipeline>(new Pipeline(typeof(IRequest), handlers));
        services.AddSingleton<IOperationReader>(new RequestReader(binders));
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
