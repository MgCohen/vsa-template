namespace VSA.Cli.Commands.Bases;

abstract class ToolCommand {
    public abstract IReadOnlyList<string> Words { get; }

    public abstract string Summary { get; }

    public virtual bool TakesName => false;

    public virtual IReadOnlyList<CommandOption> Options => [];

    public string Usage {
        get {
            var name = TakesName ? " <Name>" : "";
            var options = string.Concat(Options.Select(option => " " + option.Usage));
            return $"vsa {string.Join(' ', Words)}{name}{options}";
        }
    }

    public abstract int Run(CommandInput input);
}
