namespace Umpire.Vsa.Composition;

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;
using Umpire.Vsa.Handlers.Requests;

public static class RequestOperations {
    const string GroupMember = "Group";
    const string NameMember = "Name";
    const string EffectMember = "Effect";
    const BindingFlags Statics = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

    public static IServiceCollection AddRequestOperations(this IServiceCollection services, AppTypes types) {
        foreach (var handler in types.Implementations(typeof(RequestHandler<,>))) {
            var arguments = handler.Service.GetGenericArguments();
            services.AddSingleton(Create(arguments[0], arguments[1], types));
        }
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
        Kebab(request.Name.EndsWith("Request", StringComparison.Ordinal)
            ? request.Name.Substring(0, request.Name.Length - "Request".Length)
            : request.Name);

    public static string GroupOf(Type request, AppTypes types) {
        var space = request.Namespace ?? "";
        var module = types.Modules
            .Select(m => m.GetType())
            .Where(root => root.Namespace is { } name && (space == name || space.StartsWith(name + ".", StringComparison.Ordinal)))
            .OrderByDescending(root => root.Namespace!.Length)
            .FirstOrDefault();
        return Kebab(module is null ? AppTypes.NameOf(request) : AppTypes.NameOf(module));
    }

    static Operation Create(Type request, Type response, AppTypes types) =>
        new(StaticValue<string?>(request, GroupMember, null) ?? GroupOf(request, types),
            StaticValue<string?>(request, NameMember, null) ?? NameOf(request),
            StaticValue(request, EffectMember, Effect.Write),
            request,
            (services, input, ct) => Invoke(request, response, services, input, ct));

    static TValue StaticValue<TValue>(Type request, string member, TValue fallback) =>
        request.GetProperty(member, Statics) is { } property && property.GetValue(null) is TValue value ? value : fallback;

    static Task<OperationResult> Invoke(
        Type request, Type response, IServiceProvider services, object input, CancellationToken ct) {
        var send = typeof(RequestOperations).GetMethod(nameof(Send), BindingFlags.NonPublic | BindingFlags.Static)!;
        return (Task<OperationResult>)send.MakeGenericMethod(request, response).Invoke(null, [services, input, ct])!;
    }

    static async Task<OperationResult> Send<TRequest, TResponse>(
        IServiceProvider services, object input, CancellationToken ct)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse {
        var result = await services.GetRequiredService<IRequestSender>().SendAsync((TRequest)input, ct).ConfigureAwait(false);
        return new OperationResult(result.Value, result.Error, ExitCodeOf(result.Error, result.Value));
    }

    static int ExitCodeOf(Error? error, object? value) => error is not null ? 1 : value is IExitCode code ? code.ExitCode : 0;
}
