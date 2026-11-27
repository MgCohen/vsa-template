namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

public sealed class CommandInput {
    readonly CommandForm form;
    readonly string? name;
    readonly Dictionary<CommandOption, List<string>> values;

    CommandInput(CommandForm form, string? name, Dictionary<CommandOption, List<string>> values) {
        this.form = form;
        this.name = name;
        this.values = values;
    }

    public CommandForm Form => form;

    public string Name => name ?? throw new InvalidOperationException("This command takes no name.");

    public static CommandInput Parse(CommandForm form, IReadOnlyList<string> args) {
        var next = 0;
        string? name = null;
        if (form.TakesName) {
            if (args is [] || args[0].StartsWith('-')) {
                throw new UmpireException($"Give the name: {form.Usage}");
            }
            name = args[next++];
        }
        var values = new Dictionary<CommandOption, List<string>>();
        while (next < args.Count) {
            var option = form.Options.FirstOrDefault(o => o.Names.Contains(args[next]));
            if (option is null || next + 1 >= args.Count) {
                throw new UmpireException($"Unknown option or missing value: {args[next]}");
            }
            if (!values.TryGetValue(option, out var list)) {
                values[option] = list = [];
            }
            if (!option.Many && list.Count > 0) {
                throw new UmpireException($"Give {option.Names[0]} one time only: {form.Usage}");
            }
            list.Add(args[next + 1]);
            next += 2;
        }
        return new CommandInput(form, name, values);
    }

    public IReadOnlyList<string> All(CommandOption option) => values.TryGetValue(option, out var found) ? found : [];

    public string? Value(CommandOption option) => All(option) is [var one] ? one : null;

}
