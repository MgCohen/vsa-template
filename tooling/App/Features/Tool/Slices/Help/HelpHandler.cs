namespace Umpire.Cli.Features.Tool.Slices.Help;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Cli.Plugins.Commands.Handlers.Requests;

sealed class HelpHandler : ToolHandler<HelpRequest, HelpResponse> {
    internal static string Usage(IReadOnlyList<CommandForm> commands) {
        var listed = commands.Where(command => command.Listed).OrderBy(command => command.Order).ToList();
        var width = listed.Max(command => command.Usage.Length) + 3;
        var lines = listed.Select(command => $"  {command.Usage.PadRight(width)}{command.Summary}");
        return $"Usage:{Environment.NewLine}{string.Join(Environment.NewLine, lines)}";
    }

    protected override Task<HelpResponse> Run(HelpRequest request, CancellationToken ct) {
        if (request.Args is not ([] or ["-h" or "--help" or "help"])) {
            throw new UmpireException($"Unknown command: {string.Join(' ', request.Args)}"
                + $"{Environment.NewLine}{Environment.NewLine}{Usage(request.Commands)}");
        }
        Console.WriteLine(Usage(request.Commands));
        return Task.FromResult(new HelpResponse(0));
    }
}
