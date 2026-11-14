using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Reflection;
using VSA.Contracts;
using VSA.Contracts.Requests;
using VSA.Handlers.Requests;

namespace VSA.Host;

static class HttpApi {
    internal static void Map(WebApplication app, IEnumerable<Type> handlerServices) {
        var map = typeof(HttpApi).GetMethod(nameof(MapRequest), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var service in handlerServices) {
            map.MakeGenericMethod(service.GetGenericArguments()).Invoke(null, [app]);
        }
    }

    static void MapRequest<TRequest, TResponse>(WebApplication app)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse =>
        app.MapMethods(TRequest.Route, [TRequest.Method.Method], (Delegate)Handle<TRequest, TResponse>);

    static async Task<IResult> Handle<TRequest, TResponse>(HttpContext http)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse {
        var json = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        TRequest request;
        try {
            request = await HttpRequestBinder.Bind<TRequest>(http, json);
        } catch (Exception e) when (HttpRequestBinder.IsBindError(e)) {
            return Results.Problem(
                title: "request.not_valid", detail: e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        var result = await RequestDispatch.SendAsync(http.RequestServices, request, http.RequestAborted);
        return result.Error is { } error ? Problem(error) : Results.Ok(result.Value);
    }

    static IResult Problem(Error error) {
        var status = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Results.Problem(title: error.Code, detail: error.Message, statusCode: status);
    }
}
