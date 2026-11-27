namespace Umpire.Cli.Host;

using Umpire.Cli.Commands;

sealed record ToolLine(IReadOnlyList<string> Args) {
    public const string Help = "help";
    public const string Version = "version";

    public bool IsHelp => Args is [] or ["-h" or "--help" or Help];

    public bool IsVersion => Args is ["--version"];

    public IReadOnlyList<string> Words =>
        IsVersion ? [Version]
        : !IsHelp && CommandList.Find(Args) is { } command ? command.Words
        : [Help];
}
