namespace Umpire.Cli.Commands;

using Umpire.Cli.Commands.Bases;
using Umpire.Cli.Scaffolding;
using Umpire.Cli.Scaffolding.Setup;
using Umpire.Cli.Scaffolding.Setup.Steps;

sealed class InitCommand : ToolCommand {
    static readonly CommandOption Source = new(["--source"], "feed", Many: true);
    static readonly CommandOption Check = new(["--check"], "true");

    public override IReadOnlyList<string> Words => ["init"];

    public override string Summary =>
        "Adds the missing Umpire setup to the app in this folder. "
        + "With --check true, it shows the setup and writes nothing.";

    public override IReadOnlyList<CommandOption> Options => [Source, Check];

    public override int Run(CommandInput input) {
        input.EnsureRequiredOptions();
        var checkOnly = CheckOnly(input.Single(Check));
        var block = Environment.CurrentDirectory;
        if (Directory.GetFiles(block, "*.csproj") is []) {
            throw new UmpireException("This folder has no .csproj file. Run 'umpire init' in the folder of the root "
                + "project of the app, or make a new app with 'umpire new app <Name>'.");
        }
        var project = RootProject.Name(block);
        var version = ToolVersion.Current;
        var plan = SetupPipeline.Plan(
            new SetupContext(block, $"{project}.csproj", version, input.All(Source), keepsParentFiles: true));
        Console.WriteLine($"Setup of {project} (Umpire {version}):");
        plan.Show();
        if (checkOnly) {
            Console.WriteLine(plan.IsComplete
                ? "The setup is complete."
                : "The setup is not complete. No file is written.");
            return plan.IsComplete ? 0 : 1;
        }
        plan.Write();
        ShowByHand(plan);
        if (plan.Writes(ToolManifestStep.FileName) || plan.Writes(ToolManifestStep.InConfigFolder)) {
            Dotnet.RestoreTools(block, block);
        }
        if (plan.ByHand is not []) {
            return 1;
        }
        Console.WriteLine(plan.Files is [] ? "The setup is complete." : Next(project));
        return 0;
    }

    static void ShowByHand(SetupPlan plan) {
        if (plan.ByHand is []) {
            return;
        }
        Console.WriteLine("Do these steps by hand, then run 'umpire init' again:");
        foreach (var step in plan.ByHand) {
            Console.WriteLine($"  {step}");
        }
    }

    static string Next(string project) => $"""
        Next:
          If the app has no app class, write App/Host/{project}App.cs and call it from Program.cs (UMP030302).
          dotnet build
        """;

    static bool CheckOnly(string? value) => value switch {
        null or "false" => false,
        "true" => true,
        _ => throw new UmpireException($"Give --check true or --check false, not --check {value}."),
    };
}
