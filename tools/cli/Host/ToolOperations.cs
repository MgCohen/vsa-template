namespace Umpire.Cli.Host;

using Microsoft.Extensions.DependencyInjection;
using Umpire.Cli.Commands;
using Umpire.Cli.Commands.Bases;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

static class ToolOperations {
    public const string ErrorCode = "umpire";
    const string ToolGroup = "umpire";

    public static Operation Help { get; } = Create(ToolGroup, ToolLine.Help,
        line => line.IsHelp ? Write(CommandList.Usage) : throw CommandList.Unknown(line.Args));

    public static Operation Version { get; } = Create(ToolGroup, ToolLine.Version, _ => Write(ToolVersion.Current));

    public static Operation Of(ToolCommand command) =>
        Create(command.Words[0], command.Words[^1], line => CommandList.Run([.. line.Args]));

    static Operation Create(string group, string name, Func<ToolLine, int> run) =>
        new(group, name, Effect.Write, typeof(ToolInput), (services, _, _) => Task.FromResult(Run(services, run)));

    static OperationResult Run(IServiceProvider services, Func<ToolLine, int> run) {
        try {
            return new OperationResult(null, null, run(services.GetRequiredService<ToolLine>()));
        } catch (UmpireException e) {
            return new OperationResult(null, new Error(ErrorCode, e.Message), 1);
        } catch (Exception e) when (e is not OperationCanceledException) {
            return new OperationResult(null, new Error(ErrorCode, e.ToString()), 1);
        }
    }

    static int Write(string text) {
        Console.WriteLine(text);
        return 0;
    }
}
