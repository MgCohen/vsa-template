namespace Umpire.Cli.Commands.Bases;

sealed class CommandInput {
    readonly ToolCommand command;
    readonly string? name;
    readonly Dictionary<CommandOption, List<string>> values;

    CommandInput(ToolCommand command, string? name, Dictionary<CommandOption, List<string>> values) {
        this.command = command;
        this.name = name;
        this.values = values;
    }

    public string Name => name ?? throw new InvalidOperationException("This command takes no name.");

    public static CommandInput Parse(ToolCommand command, IReadOnlyList<string> args) {
        var next = 0;
        string? name = null;
        if (command.TakesName) {
            if (args is [] || args[0].StartsWith('-')) {
                throw new UmpireException($"Give the name: {command.Usage}");
            }
            name = args[next++];
        }
        var values = new Dictionary<CommandOption, List<string>>();
        while (next < args.Count) {
            var option = command.Options.FirstOrDefault(o => o.Names.Contains(args[next]));
            if (option is null || next + 1 >= args.Count) {
                throw new UmpireException($"Unknown option or missing value: {args[next]}");
            }
            if (!values.TryGetValue(option, out var list)) {
                values[option] = list = [];
            }
            if (!option.Many && list.Count > 0) {
                throw new UmpireException($"Give {option.Names[0]} one time only: {command.Usage}");
            }
            list.Add(args[next + 1]);
            next += 2;
        }
        return new CommandInput(command, name, values);
    }

    public void EnsureRequiredOptions() {
        var missing = command.Options.FirstOrDefault(o => o.Required && !values.ContainsKey(o));
        if (missing is not null) {
            throw new UmpireException($"Give the {missing.Value.ToLowerInvariant()}: {command.Usage}");
        }
    }

    public IReadOnlyList<string> All(CommandOption option) => values.TryGetValue(option, out var found) ? found : [];

    public string? Single(CommandOption option) => All(option) is [var one] ? one : null;
}
