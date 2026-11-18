namespace Umpire.Hosts.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

static class HttpApi {
    internal static void Map(WebApplication app, Func<Operation, string> route) {
        foreach (var operation in app.Services.GetServices<Operation>()) {
            app.MapMethods(route(operation), [MethodOf(operation.Effect)],
                (Func<HttpContext, Task<IResult>>)(http => Handle(http, operation)));
        }
    }

    internal static string MethodOf(Effect effect) => effect switch {
        Effect.Read => HttpMethods.Get,
        Effect.Delete => HttpMethods.Delete,
        _ => HttpMethods.Post,
    };

    static async Task<IResult> Handle(HttpContext http, Operation operation) {
        var json = http.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;
        object input;
        try {
            input = await HttpRequestBinder.Bind(operation.Input, http, json);
        } catch (Exception e) when (HttpRequestBinder.IsBindError(e)) {
            return Results.Problem(
                title: "request.not_valid", detail: e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        var result = await operation.Invoke(http.RequestServices, input, http.RequestAborted);
        return result.Error is { } error ? Problem(error) : Results.Ok(result.Value);
    }

    static IResult Problem(Error error) {
        var status = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Results.Problem(title: error.Code, detail: error.Message, statusCode: status);
    }
}
