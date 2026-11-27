namespace Umpire.Cli.Plugins.Commands.Domain.Shared;

public sealed record CommandForm(IReadOnlyList<string> Words, string Summary, int Order) {
    public bool TakesName { get; init; }

    public IReadOnlyList<CommandOption> Options { get; init; } = [];

    public IReadOnlyList<string>? ExactLine { get; init; }

    public bool Listed { get; init; } = true;

    public string Group => Words[0];

    public string Name => Words[^1];

    public string Usage {
        get {
            if (ExactLine is { } line) {
                return $"umpire {string.Join(' ', line)}";
            }
            var name = TakesName ? " <Name>" : "";
            var options = string.Concat(Options.Select(option => " " + option.Usage));
            return $"umpire {string.Join(' ', Words)}{name}{options}";
        }
    }

    public bool Selects(IReadOnlyList<string> args) =>
        ExactLine is { } line
            ? args.SequenceEqual(line)
            : args.Count >= Words.Count && Words.SequenceEqual(args.Take(Words.Count));

    public string Missing(CommandOption option) => $"Give the {option.Value.ToLowerInvariant()}: {Usage}";
}
