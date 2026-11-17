namespace Umpire.Vsa.Composition;

using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Commands;

sealed class ConsoleEntry<TResult>(
    Type command,
    IServiceProvider services,
    IConfiguration configuration,
    IHostApplicationLifetime lifetime,
    ConsoleExit exit) : IHostedService {
    public async Task StartAsync(CancellationToken cancellationToken) {
        try {
            var message = (ICommand<TResult>)Bind();
            using var scope = services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ICommandSender>();
            exit.Code = ExitCodeOf(await sender.SendAsync(message, cancellationToken).ConfigureAwait(false));
        } catch (Exception e) when (e is not OperationCanceledException) {
            var reason = (e as TargetInvocationException)?.InnerException?.Message ?? e.Message;
            await Console.Error.WriteLineAsync($"{command.Name} failed: {reason}").ConfigureAwait(false);
            exit.Code = 1;
        } finally {
            lifetime.StopApplication();
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    static int ExitCodeOf(Result<TResult> result) {
        if (result.Error is { } error) {
            Console.Error.WriteLine($"{error.Code}: {error.Message}");
            return 1;
        }
        return result.Value is IExitCode code ? code.ExitCode : 0;
    }

    object Bind() {
        var constructor = command.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var values = constructor.GetParameters().Select(Value).ToArray();
        return constructor.Invoke(values);
    }

    object? Value(ParameterInfo parameter) {
        var text = configuration[parameter.Name!];
        if (text is null) {
            return parameter.HasDefaultValue
                ? parameter.DefaultValue
                : throw new InvalidOperationException($"{command.Name} needs --{parameter.Name}. Add it to the command line.");
        }
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type.IsEnum) {
            return Enum.GetNames(type).FirstOrDefault(name => string.Equals(name, text, StringComparison.OrdinalIgnoreCase)) is { } name
                ? Enum.Parse(type, name)
                : throw new InvalidOperationException(
                    $"--{parameter.Name} must be one of {string.Join(", ", Enum.GetNames(type))}. The value is '{text}'.");
        }
        return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
    }
}
