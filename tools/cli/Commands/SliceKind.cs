using Umpire.Cli.Scaffolding;

namespace Umpire.Cli.Commands;

sealed record SliceKind(
    string Noun,
    Templates.Kind Template,
    string SampleName,
    IReadOnlyList<string> Suffixes,
    string Parts) {
    public static readonly SliceKind Request = new(
        "request",
        Templates.Kind.Request,
        Templates.Request,
        ["Request", "Response", "Handler"],
        "a request, its response and its handler");

    public static readonly SliceKind Command = new(
        "command",
        Templates.Kind.Command,
        Templates.Command,
        ["Command", "Result", "Handler"],
        "a command, its result and its handler");

    public static IReadOnlyList<SliceKind> All { get; } = [Request, Command];
}
