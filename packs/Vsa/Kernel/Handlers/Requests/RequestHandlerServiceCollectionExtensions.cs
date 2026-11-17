namespace Umpire.Vsa.Handlers.Requests;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Kernel;
using Umpire.Messages.Handlers;

public static class RequestHandlerServiceCollectionExtensions {
    public static IServiceCollection AddRequestHandlers(this IServiceCollection services, AppTypes types) {
        EnsureOneHandlerForEachRequest(types.Implementations(typeof(RequestHandler<,>)));
        services.AddScoped<IRequestSender, InProcessRequestSender>();
        services.AddPipeline(new PipelineBuilder<IMessage>().HandlersOf(typeof(RequestHandler<,>)));
        return services;
    }

    static void EnsureOneHandlerForEachRequest(IReadOnlyList<ServiceImplementation> handlers) {
        foreach (var request in handlers.GroupBy(h => h.Service).Where(g => g.Count() > 1)) {
            var names = request.Select(h => h.Implementation.FullName).ToArray();
            throw new InvalidOperationException(
                $"Request {request.Key.GetGenericArguments()[0].FullName} has two handlers: "
                + $"{names[0]} and {names[1]}. A request has one handler.");
        }
    }
}
