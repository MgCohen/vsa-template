namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;

static class CommandList {
    const string VersionUsage = "umpire --version";
    const string VersionSummary = "Shows the version of the tool.";

    public static IReadOnlyList<ToolCommand> All { get; } = [
        new NewAppCommand(),
        new InitCommand(),
        new AddModuleCommand(),
        new AddPluginCommand(),
        new AddPackCommand(),
        .. SliceKind.All.Select(kind => new AddSliceCommand(kind)),
        new DoctorCommand(),
        new UpdateCommand(),
    ];

    public static string Usage {
        get {
            var width = All.Max(command => command.Usage.Length) + 3;
            var lines = All.Select(command => $"  {command.Usage.PadRight(width)}{command.Summary}")
                .Append($"  {VersionUsage.PadRight(width)}{VersionSummary}");
            return $"Usage:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
        }
    }

    public static ToolCommand? Find(IReadOnlyList<string> args) =>
        All.FirstOrDefault(command =>
            args.Count >= command.Words.Count && command.Words.SequenceEqual(args.Take(command.Words.Count)));

    public static int Run(string[] args) {
        var command = Find(args) ?? throw new UmpireException(
            $"Unknown command: {string.Join(' ', args)}{Environment.NewLine}{Environment.NewLine}{Usage}");
        return command.Run(CommandInput.Parse(command, args[command.Words.Count..]));
    }
}
