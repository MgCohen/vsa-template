using Microsoft.AspNetCore.Http;
using VSA.Domain;

namespace VSA.Host;

public static class ResultHttpExtensions {
    // Success: 200 with the value. Error: a problem response, 404 if the code ends with ".not_found", else 400.
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                title: result.Error!.Code,
                detail: result.Error.Message,
                statusCode: result.Error.Code.EndsWith(".not_found", StringComparison.Ordinal) ? 404 : 400);
}
