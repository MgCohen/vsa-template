namespace Umpire.Cli.Host;

using System.Globalization;
using System.Reflection;
using Umpire.Cli.Plugins.Commands.Contracts.Requests;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Kernels;

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

    public IReadOnlyCollection<string> Names => [.. all.Select(command => KeyOf(command.Form))];

    public bool Accepts(Type message) => all.Any(command => command.Request == message);

    public Operation OperationOf(IReadOnlyList<string> line) {
        var values = new Dictionary<string, string>();
        for (var i = 0; i < line.Count; i++) {
            values[i.ToString(CultureInfo.InvariantCulture)] = line[i];
        }
        return new Operation(KeyOf(Select(line)), values);
    }

    public static IReadOnlyList<string> LineOf(Operation operation) =>
        [.. operation.Values
            .Select(pair => new { Index = int.Parse(pair.Key, CultureInfo.InvariantCulture), pair.Value })
            .OrderBy(pair => pair.Index)
            .Select(pair => pair.Value)];

    static bool IsToolRequest(Type type) =>
        type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IToolRequest<>));

    ToolCommand Find(IReadOnlyList<string> line) =>
        all.FirstOrDefault(command => command.Form.Selects(line))
        ?? all.FirstOrDefault(command => command.Form.Words is [Help])
        ?? throw new InvalidOperationException("The tool has no help request.");

    static string KeyOf(CommandForm form) => $"{form.Group}/{form.Name}";

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
