using System.Reflection;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using VSA.Contracts.Requests;
using VSA.Domain;

namespace VSA.Handlers.Requests;

// One handler for each request. The handler is also the FastEndpoints endpoint of the request.
// The method and the route come from the request type, so a handler does not configure them.
public abstract class RoutedHandler<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : Request<TResponse>, IRoutedRequest {
    public sealed override void Configure() {
        Verbs(TRequest.Method);
        Routes(TRequest.Route);
        AllowAnonymous();
        // FastEndpoints cannot bind a request with no properties, for example ListNotesRequest.
        if (typeof(TRequest).GetProperties(BindingFlags.Public | BindingFlags.Instance).Length == 0) {
            RequestBinder(new NoPropertiesBinder());
        }
    }

    // Success: 200 with the response. Error: a problem response, 404 if the code ends with ".not_found", else 400.
    public sealed override async Task HandleAsync(TRequest request, CancellationToken ct) {
        var result = await HandleRequestAsync(request, ct);
        if (result.IsSuccess) {
            await Send.OkAsync(result.Value, ct);
            return;
        }
        var error = result.Error!;
        var status = error.Code.EndsWith(".not_found", StringComparison.Ordinal) ? 404 : 400;
        await Send.ResultAsync(TypedResults.Problem(title: error.Code, detail: error.Message, statusCode: status));
    }

    protected abstract Task<Result<TResponse>> HandleRequestAsync(TRequest request, CancellationToken ct);

    sealed class NoPropertiesBinder : IRequestBinder<TRequest> {
        public ValueTask<TRequest> BindAsync(BinderContext ctx, CancellationToken ct) => new(Activator.CreateInstance<TRequest>());
    }
}
