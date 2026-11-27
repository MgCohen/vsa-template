namespace Umpire.Cli.Features.Scaffold.Slices.NewApp;

using Umpire.Cli.Features.Scaffold.Domain;
using Umpire.Cli.Features.Scaffold.Domain.Setup;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;
using Umpire.Files;

sealed class NewAppHandler : ToolHandler<NewAppRequest, NewAppResponse> {
    internal static IReadOnlyList<TextFile> Files(string name, IReadOnlyList<string> sources, string version) =>
        Plan(name, sources, version).Files;

    internal static SetupPlan Plan(string name, IReadOnlyList<string> sources, string version) =>
        SetupTasks.Plan(Context(name, Path.GetFullPath(name), sources, version, central: false));

    protected override Task<NewAppResponse> Run(NewAppRequest request, CancellationToken ct) {
        var name = Names.Check(request.Name, "app");
        var block = Path.GetFullPath(name);
        var version = ToolVersion.Current;
        var central = CentralVersions.Above(Path.GetDirectoryName(block)!);
        var plan = SetupTasks.Plan(Context(name, block, request.Sources, version, central));
        Console.WriteLine($"Making the app {name} (Umpire {version}):");
        NewFiles.Write(
            block,
            plan.Files,
            $"{Environment.NewLine}To add Umpire to an existing app, run 'umpire init' in its folder.");
        Console.WriteLine($"""

            Next:
              cd {name}
              dotnet tool exec Umpire.Cli@{version} -y -- add module <Name>
            """);
        return Task.FromResult(new NewAppResponse(0));
    }

    static SetupContext Context(
        string name, string block, IReadOnlyList<string> sources, string version, bool central) {
        var context = new SetupContext(block, $"{name}.csproj", version, sources, existingApp: false);
        var names = new Dictionary<string, string> { [Templates.App] = name };
        var files = Templates.Files(TemplateGroup.App, names, version);
        foreach (var file in Templates.WithCentralVersions(files, central)) {
            context.Add(file);
        }
        return context;
    }
}
