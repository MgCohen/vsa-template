namespace Umpire.Vsa.Composition;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

public static class RequestOperations {
    public static IServiceCollection AddRequestOperations(this IServiceCollection services, AppTypes types) {
#if NET
        var create = typeof(RequestOperations).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var handler in types.Implementations(typeof(RequestHandler<,>))) {
            var arguments = handler.Service.GetGenericArguments();
            var operation = (Operation)create.MakeGenericMethod(arguments).Invoke(null, [types])!;
            services.AddSingleton(operation);
        }
#endif
        return services;
    }

    public static string Kebab(string name) {
        var text = new System.Text.StringBuilder();
        for (var i = 0; i < name.Length; i++) {
            if (char.IsUpper(name[i]) && i > 0) {
                text.Append('-');
            }
            text.Append(char.ToLowerInvariant(name[i]));
        }
        return text.ToString();
    }

    public static string NameOf(Type request) =>
        Kebab(request.Name.EndsWith("Request", StringComparison.Ordinal) ? request.Name[..^"Request".Length] : request.Name);

    public static string GroupOf(Type request, AppTypes types) {
        var space = request.Namespace ?? "";
        var module = types.Modules
            .Select(m => m.GetType())
            .Where(root => root.Namespace is { } name && (space == name || space.StartsWith(name + ".", StringComparison.Ordinal)))
            .OrderByDescending(root => root.Namespace!.Length)
            .FirstOrDefault();
        return Kebab(module is null ? AppTypes.NameOf(request) : AppTypes.NameOf(module));
    }

#if NET
    static Operation Create<TRequest, TResponse>(AppTypes types)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse =>
        new(TRequest.Group ?? GroupOf(typeof(TRequest), types),
            TRequest.Name ?? NameOf(typeof(TRequest)),
            TRequest.Effect,
            typeof(TRequest),
            Invoke<TRequest, TResponse>);

    static async Task<OperationResult> Invoke<TRequest, TResponse>(
        IServiceProvider services, object input, CancellationToken ct)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse {
        var result = await services.GetRequiredService<IRequestSender>().SendAsync((TRequest)input, ct).ConfigureAwait(false);
        return new OperationResult(result.Value, result.Error, ExitCodeOf(result.Error, result.Value));
    }

    static int ExitCodeOf(Error? error, object? value) => error is not null ? 1 : value is IExitCode code ? code.ExitCode : 0;
#endif
}
