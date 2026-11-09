using Microsoft.Extensions.DependencyInjection;
using VSA.Contracts;
using VSA.Module;

namespace VSA.Handlers.Requests;

static class RequestHandlerInstaller {
    internal static void Install(IServiceCollection services, ModuleTypes types) {
        var handlers = types.Implementations(typeof(RequestHandler<,>));
        EnsureOneHandlerForEachRequest(handlers);
        services.AddDefaultPipeline(
            PipelineKind.Requests, new PipelineBuilder<IMessage>().HandlersOf(typeof(RequestHandler<,>)));
        foreach (var (service, handler) in handlers) {
            services.AddScoped(service, handler);
        }
    }

    static void EnsureOneHandlerForEachRequest(IReadOnlyList<(Type Service, Type Implementation)> handlers) {
        foreach (var request in handlers.GroupBy(h => h.Service).Where(g => g.Count() > 1)) {
            var names = request.Select(h => h.Implementation.FullName).ToArray();
            throw new InvalidOperationException(
                $"Request {request.Key.GetGenericArguments()[0].FullName} has two handlers: "
                + $"{names[0]} and {names[1]}. A request has one handler.");
        }
    }
}
