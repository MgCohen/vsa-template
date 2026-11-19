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
        return type.IsEnum
            ? EnumValue(type, parameter.Name!, text)
            : Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
    }

    static object EnumValue(Type type, string option, string text) {
        var names = Enum.GetNames(type);
        string[] parts = type.IsDefined(typeof(FlagsAttribute), false)
            ? text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            : [text];
        var found = parts.Select(part => Member(names, part)).ToList();
        if (found.Count == 0 || found.Contains(null)) {
            throw new InvalidOperationException(
                $"--{option} must be one of {string.Join(", ", names)}. The value is '{text}'.");
        }
        var value = found.Aggregate(0L, (all, name) => all | Number(Enum.Parse(type, name!)));
        return Enum.ToObject(type, value);
    }

    static string? Member(string[] names, string part) =>
        names.FirstOrDefault(name => string.Equals(name, part, StringComparison.OrdinalIgnoreCase));

    static long Number(object member) => Convert.ToInt64(member, CultureInfo.InvariantCulture);
}
