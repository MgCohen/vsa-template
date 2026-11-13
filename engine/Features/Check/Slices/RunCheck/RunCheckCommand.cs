using VSA.Contracts.Commands;

namespace Check.Slices.RunCheck;

public sealed record RunCheckCommand(string Root, string Bin, string Mode = "Error", string Skip = "", string Off = "")
    : IConsoleCommand<RunCheckResult>;
