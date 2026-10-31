using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;

namespace VSA.Host;

public static class RequestEndpoints {
    // GET and DELETE bind from the route and the query. Other methods bind from the body.
    public static RouteHandlerBuilder MapRequest<TRequest, TResponse>(this IEndpointRouteBuilder app)
        where TRequest : Request<TResponse> {
        var route = typeof(TRequest).GetCustomAttribute<HttpRouteAttribute>()
            ?? throw new InvalidOperationException($"{typeof(TRequest).Name} has no [HttpRoute].");
        return route.Method is Http.Get or Http.Delete
            ? app.MapMethods(route.Route, [route.Method],
                async ([AsParameters] TRequest request, RequestHandler<TRequest, TResponse> handler, CancellationToken ct) =>
                    (await handler.HandleAsync(request, ct)).ToHttpResult())
            : app.MapMethods(route.Route, [route.Method],
                async ([FromBody] TRequest request, RequestHandler<TRequest, TResponse> handler, CancellationToken ct) =>
                    (await handler.HandleAsync(request, ct)).ToHttpResult());
    }
}
