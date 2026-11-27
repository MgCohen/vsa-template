namespace Umpire.Cli.Host;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Kernels;
using Umpire.Messages.Contracts;

public sealed class ToolConverter(ToolCommands commands) : IOperationConverter {
    public IReadOnlyCollection<string> Names => commands.Names;

    public bool Accepts(Type message) => commands.Accepts(message);

    public Result<object> Convert(Operation operation) {
        try {
            return commands.Request(ToolCommands.LineOf(operation));
        } catch (UmpireException e) {
            return new Error(UmpireException.Code, e.Message);
        }
    }
}
