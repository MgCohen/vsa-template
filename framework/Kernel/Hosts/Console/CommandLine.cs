namespace Umpire.Hosts.Console;

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Umpire.Kernels;

static class CommandLine {
    internal static string[] Options(string[] args) => [.. args.SkipWhile(IsWord)];

    internal static Operation Select(IReadOnlyList<Operation> operations, string[] args) {
        var words = args.TakeWhile(IsWord).ToList();
        var found = words switch {
            [] => operations,
            [var name] => [.. operations.Where(o => o.Name == name)],
            [var group, var name, ..] => [.. operations.Where(o => o.Group == group && o.Name == name)],
        };
        var names = string.Join(", ", operations.Select(o => $"{o.Group} {o.Name}").Order(StringComparer.Ordinal));
        if (found.Count == 1) {
            return found[0];
        }
        throw new InvalidOperationException(operations.Count == 0
            ? "The console host found no request. Add a request and its handler."
            : $"Name one request on the command line: {names}.");
    }

    internal static async Task Run(
        IServiceProvider services, Operation operation, IConfiguration configuration, AppStop stop,
        CancellationToken ct) {
        try {
            var input = Bind(operation.Input, configuration);
            using var scope = services.CreateScope();
            var result = await operation.Invoke(scope.ServiceProvider, input, ct).ConfigureAwait(false);
            if (result.Error is { } error) {
                await System.Console.Error.WriteLineAsync($"{error.Code}: {error.Message}").ConfigureAwait(false);
            }
            stop.Request(result.ExitCode);
        } catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested) {
            var reason = (e as TargetInvocationException)?.InnerException?.Message ?? e.Message;
            await System.Console.Error.WriteLineAsync($"{operation.Name} failed: {reason}").ConfigureAwait(false);
            stop.Request(1);
        }
    }

    static bool IsWord(string arg) => !arg.StartsWith('-');

    static object Bind(Type input, IConfiguration configuration) {
        var constructor = input.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var values = constructor.GetParameters().Select(p => Value(input, p, configuration)).ToArray();
        return constructor.Invoke(values);
    }

    static object? Value(Type input, ParameterInfo parameter, IConfiguration configuration) {
        var text = configuration[parameter.Name!];
        if (text is null) {
            return parameter.HasDefaultValue
                ? parameter.DefaultValue
                : throw new InvalidOperationException(
                    $"{input.Name} needs --{parameter.Name}. Add it to the command line.");
        }
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type.IsEnum) {
            var names = Enum.GetNames(type);
            return names.FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) is { } name
                ? Enum.Parse(type, name)
                : throw new InvalidOperationException(
                    $"--{parameter.Name} must be one of {string.Join(", ", names)}. The value is '{text}'.");
        }
        return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
    }
}
