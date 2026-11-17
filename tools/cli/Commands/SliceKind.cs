namespace Umpire.Cli.Commands;

using Umpire.Cli.Scaffolding;

sealed record SliceKind(
    string Noun,
    TemplateGroup Template,
    string SampleName,
    IReadOnlyList<string> Suffixes,
    string Parts) {
    public static readonly SliceKind Request = new(
        "request",
        TemplateGroup.Request,
        Templates.Request,
        ["Request", "Response", "Handler"],
        "a request, its response and its handler");

    public static readonly SliceKind Command = new(
        "command",
        TemplateGroup.Command,
        Templates.Command,
        ["Command", "Result", "Handler"],
        "a command, its result and its handler");

    public static IReadOnlyList<SliceKind> All { get; } = [Request, Command];
}
