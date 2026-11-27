namespace Spike.Pack;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spike.Core;

public sealed class PackKernel : Kernel {
    protected override void Configure(IServiceCollection services, Assembly app) {
        var handlers = new Dictionary<Type, HandlerCall>();
        var requests = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in app.GetTypes().Where(t => !t.IsAbstract && t.BaseType is { IsGenericType: true } b
            && b.GetGenericTypeDefinition() == typeof(RequestHandler<,>))) {
            var arguments = type.BaseType!.GetGenericArguments();
            services.AddScoped(type);
            handlers[arguments[0]] = (HandlerCall)typeof(PackKernel).GetMethod(nameof(Call), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod(type, arguments[0], arguments[1]).Invoke(null, null)!;
            requests[arguments[0].Name.Replace("Request", "", StringComparison.Ordinal)] = arguments[0];
        }
        services.AddSingleton<IPipeline>(provider => new Pipeline(handlers, provider.GetServices<PipelineEntry>()));
        services.AddSingleton<IOperationConverter>(new RequestConverter(requests));
    }

    static HandlerCall Call<THandler, TRequest, TResponse>()
        where THandler : RequestHandler<TRequest, TResponse>
        where TRequest : IRequest<TResponse> =>
        async (scope, message, ct) => await scope.GetRequiredService<THandler>().Handle((TRequest)message, ct).ConfigureAwait(false);
}
