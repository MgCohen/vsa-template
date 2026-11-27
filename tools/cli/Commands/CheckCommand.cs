namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class CheckCommand : AppCommand {
    static readonly CommandOption Configuration = new(["-c", "--configuration"], "configuration");
    static readonly CommandOption Mode = new(["--mode"], "Warn|Error");

    public override IReadOnlyList<string> Words => ["check"];

    public override string Summary =>
        "Builds the app and checks it with the rule engine of the app (the version of its Umpire.Engine package).";

    public override IReadOnlyList<CommandOption> Options => [Configuration, Mode];

    protected override int Run(string block, CommandInput input) {
        var engine = AppEngine.Of(block, input.Single(Configuration));
        Console.WriteLine($"Checking {block} with the rule engine {engine.File}:");
        return Dotnet.Stream(block, engine.CheckArguments(block, input.Single(Mode)));
    }
}
