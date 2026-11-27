namespace Umpire.Hosts.Web;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

static class HttpApi {
    internal const string NameKey = "name";

    static readonly string[] Methods = [HttpMethods.Get, HttpMethods.Post, HttpMethods.Put, HttpMethods.Patch, HttpMethods.Delete];

    internal static void Map(WebApplication app, string pattern) =>
        app.MapMethods(pattern, Methods, (Func<HttpContext, Task<IResult>>)Handle);

    static async Task<IResult> Handle(HttpContext http) {
        var name = http.Request.RouteValues.TryGetValue(NameKey, out var route) ? route?.ToString() ?? "" : "";
        IReadOnlyDictionary<string, string> values;
        try {
            values = await HttpValues.Read(http.Request, http.RequestAborted);
        } catch (Exception e) when (HttpValues.IsReadError(e)) {
            return Results.Problem(title: "request.not_valid", detail: e.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        var result = await http.RequestServices.SendAsync(new Operation(name, values), http.RequestAborted);
        return result.Error is { } error ? Problem(error) : Results.Ok(result.Value);
    }

    static IResult Problem(Error error) {
        var status = error.Code.EndsWith(".not_found", StringComparison.Ordinal)
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Results.Problem(title: error.Code, detail: error.Message, statusCode: status);
    }
}
