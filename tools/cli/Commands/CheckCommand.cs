namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;

sealed class CheckCommand : AppCommand {
    static readonly CommandOption Configuration = new(["-c", "--configuration"], "Debug|Release");
    static readonly string[] Configurations = ["Debug", "Release"];
    static readonly CommandOption Mode = new(["--mode"], "Warn|Error");

    public override IReadOnlyList<string> Words => ["check"];

    public override string Summary =>
        "Builds the app and checks it with the rule engine of the app (the version of its Umpire.Engine package).";

    public override IReadOnlyList<CommandOption> Options => [Configuration, Mode];

    protected override int Run(string block, CommandInput input) {
        var configuration = input.Single(Configuration);
        if (configuration is not null && !Configurations.Contains(configuration, StringComparer.OrdinalIgnoreCase)) {
            throw new UmpireException($"Give -c Debug or -c Release, not -c {configuration}. "
                + "The rule engine finds the configuration in the output folder, and it knows only these two.");
        }
        var engine = AppEngine.Of(block, configuration);
        Console.WriteLine($"Checking {block} with the rule engine {engine.File}:");
        return Dotnet.Stream(block, engine.CheckArguments(block, input.Single(Mode)));
    }
}
