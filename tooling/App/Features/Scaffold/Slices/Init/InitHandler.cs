namespace Umpire.Cli.Features.Scaffold.Slices.Init;

using Umpire.Cli.Features.Scaffold.Domain.Setup;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class InitHandler : ToolHandler<InitRequest, InitResponse> {
    protected override Task<InitResponse> Run(InitRequest request, CancellationToken ct) =>
        Task.FromResult(new InitResponse(Init(request)));

    static int Init(InitRequest request) {
        var checkOnly = CheckOnly(request.Check);
        var block = Environment.CurrentDirectory;
        if (Directory.GetFiles(block, "*.csproj") is []) {
            throw new UmpireException("This folder has no .csproj file. Run 'umpire init' in the folder of the root "
                + "project of the app, or make a new app with 'umpire new app <Name>'.");
        }
        var project = RootProject.Name(block);
        var version = ToolVersion.Current;
        var plan = SetupTasks.Plan(
            new SetupContext(block, $"{project}.csproj", version, request.Sources, existingApp: true));
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
        Console.WriteLine("Do these tasks by hand, then run 'umpire init' again:");
        foreach (var task in plan.ByHand) {
            Console.WriteLine($"  {task}");
        }
    }

    static string Next(string project) => $"""
        Next:
          If the app has no app class, write App/{project}App.cs, and move Program.cs to App/Program.cs (UMP030502).
          dotnet build
        """;

    static bool CheckOnly(string? value) => value switch {
        null or "false" => false,
        "true" => true,
        _ => throw new UmpireException($"Give --check true or --check false, not --check {value}."),
    };
}
