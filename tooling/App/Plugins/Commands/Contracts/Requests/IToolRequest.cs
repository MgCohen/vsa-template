namespace Umpire.Cli.Plugins.Commands.Contracts.Requests;

using Umpire.Cli.Plugins.Commands.Domain.Shared;
using Umpire.Vsa.Contracts.Requests;

public interface IToolRequest<TSelf> : IRequest where TSelf : IToolRequest<TSelf> {
    static abstract CommandForm Form { get; }

    static abstract TSelf From(ToolCall call);
}
