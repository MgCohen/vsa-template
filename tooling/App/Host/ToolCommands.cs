namespace Umpire.Cli.Host;

using System.Reflection;
using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

public sealed class ToolCommands {
    public const string Help = "help";

    readonly IReadOnlyList<ToolCommand> all;

    ToolCommands(IReadOnlyList<ToolCommand> all) {
        this.all = all;
    }

    public IReadOnlyList<CommandForm> Forms => [.. all.Select(command => command.Form)];

    public static ToolCommands Of(IReadOnlyList<Type> requests) =>
        new([.. requests.Where(IsToolRequest).Select(ToolCommand.Of).OrderBy(command => command.Form.Order)]);

    public CommandForm Select(IReadOnlyList<string> line) => Find(line).Form;

    public object Request(IReadOnlyList<string> line) => Find(line).Create(new ToolCall(line, Forms));

    public bool CancelsOnSignal(IReadOnlyList<string> line) =>
        all.Count > 0 && typeof(IChildProcessRequest).IsAssignableFrom(Find(line).Request);

    public Operation? Wrap(Operation operation, Func<IReadOnlyList<string>> line) {
        var command = all.FirstOrDefault(c => c.Request == operation.Input);
        if (command is null) {
            return null;
        }
        return operation with {
            Group = command.Form.Group,
            Name = command.Form.Name,
            Input = typeof(ToolInput),
            Invoke = (services, _, ct) => Invoke(operation, line(), services, ct),
        };
    }

    static bool IsToolRequest(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IToolRequest<>));

    ToolCommand Find(IReadOnlyList<string> line) =>
        all.FirstOrDefault(command => command.Form.Selects(line))
        ?? all.FirstOrDefault(command => command.Form.Words is [Help])
        ?? throw new InvalidOperationException("The tool has no help request.");

    Task<OperationResult> Invoke(
        Operation operation, IReadOnlyList<string> line, IServiceProvider services, CancellationToken ct) {
        object request;
        try {
            request = Request(line);
        } catch (UmpireException e) {
            return Task.FromResult(new OperationResult(null, new Error(UmpireException.Code, e.Message), 1));
        }
        return operation.Invoke(services, request, ct);
    }

    sealed record ToolCommand(Type Request, CommandForm Form, Func<ToolCall, object> Create) {
        const BindingFlags Statics = BindingFlags.NonPublic | BindingFlags.Static;

        public static ToolCommand Of(Type request) => new(
            request,
            Method(nameof(FormOf), request).CreateDelegate<Func<CommandForm>>()(),
            Method(nameof(CreateOf), request).CreateDelegate<Func<ToolCall, object>>());

        static MethodInfo Method(string name, Type request) =>
            typeof(ToolCommand).GetMethod(name, Statics)!.MakeGenericMethod(request);

        static CommandForm FormOf<TRequest>() where TRequest : IToolRequest<TRequest> => TRequest.Form;

        static object CreateOf<TRequest>(ToolCall call) where TRequest : IToolRequest<TRequest> => TRequest.From(call)!;
    }
}
