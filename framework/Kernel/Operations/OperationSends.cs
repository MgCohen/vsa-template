namespace Umpire.Kernels;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Messages.Contracts;

public static class OperationSends {
    public const string NoConverterCode = "operation.no_converter";

    public static async Task<IMessageResult> SendAsync(this IServiceProvider scope, Operation operation, CancellationToken ct = default) {
        if (scope.GetService<IOperationConverter>() is not { } converter) {
            return (Result<Unit>)new Error(NoConverterCode,
                $"The app has no operation converter, so '{operation.Name}' has no message. Its kernel registers one.");
        }
        var converted = converter.Convert(operation);
        if (converted.Error is { } error) {
            return (Result<Unit>)error;
        }
        return await scope.GetRequiredService<IMessageHub>().SendAsync(converted.Value, ct).ConfigureAwait(false);
    }

    public static string? NoConverterWarning(IServiceProvider services) =>
        services.GetService<IOperationConverter>() is null
            ? "The app has no operation converter, so a host cannot reach its messages. The kernel of the app registers one."
            : null;
}
