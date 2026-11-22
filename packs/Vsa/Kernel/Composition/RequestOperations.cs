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

    public static IServiceCollection AddRequestOperations(this IServiceCollection services, AppTypes types) {
        var create = typeof(RequestOperations).GetMethod(nameof(Create), BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var handler in types.Implementations(typeof(RequestHandler<,>))) {
            var arguments = handler.Service.GetGenericArguments();
            var operation = (Operation)create.MakeGenericMethod(arguments).Invoke(null, [types])!;
            services.AddSingleton(operation);
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

    static Operation Create<TRequest, TResponse>(AppTypes types)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse =>
        new(GroupValue<TRequest>() ?? GroupOf(typeof(TRequest), types),
            NameValue<TRequest>() ?? NameOf(typeof(TRequest)),
            EffectValue<TRequest>(),
            typeof(TRequest),
            Invoke<TRequest, TResponse>);

    static string? GroupValue<TRequest>() => StaticValue<string?>(typeof(TRequest), GroupMember, null);

    static string? NameValue<TRequest>() => StaticValue<string?>(typeof(TRequest), NameMember, null);

    static Effect EffectValue<TRequest>() => StaticValue(typeof(TRequest), EffectMember, Effect.Write);

    static TValue StaticValue<TValue>(Type request, string member, TValue fallback) =>
        request.GetProperty(member, BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            is { } property && property.GetValue(null) is TValue value
            ? value
            : fallback;

    static async Task<OperationResult> Invoke<TRequest, TResponse>(
        IServiceProvider services, object input, CancellationToken ct)
        where TRequest : IRequest<TResponse>
        where TResponse : IResponse {
        var result = await services.GetRequiredService<IRequestSender>().SendAsync((TRequest)input, ct).ConfigureAwait(false);
        return new OperationResult(result.Value, result.Error, ExitCodeOf(result.Error, result.Value));
    }

    static int ExitCodeOf(Error? error, object? value) => error is not null ? 1 : value is IExitCode code ? code.ExitCode : 0;
}
