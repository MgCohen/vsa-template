namespace Umpire.Cli.Plugins.Commands.Contracts.Requests;

using Umpire.Messages.Contracts;
using Umpire.Vsa.Contracts.Requests;

public abstract record ToolResponse(int ExitCode) : IResponse, IExitCode;
