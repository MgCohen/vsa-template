namespace Umpire.Cli.Features.Checks.Slices.Check;

using Umpire.Cli.Features.Checks.Infrastructure;
using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;
using Umpire.Cli.Plugins.Commands.Infrastructure;

sealed class CheckHandler : ToolHandler<CheckRequest, CheckResponse> {
    static readonly string[] configurations = ["Debug", "Release"];

    protected override async Task<CheckResponse> Run(CheckRequest request, CancellationToken ct) {
        var block = RootProject.AppOfCurrentFolder();
        var configuration = request.Configuration;
        if (configuration is not null && !configurations.Contains(configuration, StringComparer.OrdinalIgnoreCase)) {
            throw new UmpireException($"Give -c Debug or -c Release, not -c {configuration}. "
                + "The rule engine finds the configuration in the output folder, and it knows only these two.");
        }
        var engine = AppEngine.Of(block, configuration);
        Console.WriteLine($"Checking {block} with the rule engine {engine.File}:");
        var exitCode = await Dotnet.Stream(block, engine.CheckArguments(block, request.Mode), ct).ConfigureAwait(false);
        return new CheckResponse(exitCode);
    }
}
