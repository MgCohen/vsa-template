namespace Umpire.Cli.Features.Scaffold.Domain.Setup;

using Umpire.Files;

sealed class SetupPlan(SetupContext context, IReadOnlyList<TaskResult> tasks) {
    public IReadOnlyList<TaskResult> Tasks => tasks;

    public IReadOnlyList<TextFile> Files => [.. context.Planned];

    public IReadOnlyList<string> ByHand =>
        [.. tasks.Select(task => task.Result is SetupResult.ByHand hand ? $"{task.Id}: {hand.Message}" : null)
            .OfType<string>()];

    public bool IsComplete => tasks.All(task => task.Result is SetupResult.Done);

    public void Show() {
        var width = tasks.Max(task => task.Id.Length) + 2;
        foreach (var task in tasks) {
            Console.WriteLine($"  {task.Id.PadRight(width)}{Shown(task.Result)}");
        }
    }

    public void Write() {
        foreach (var file in context.Planned) {
            Console.WriteLine($"  {(file.Write(context.Block) ? "created" : "changed")} {file.Path}");
        }
    }

    string Shown(SetupResult result) => result switch {
        SetupResult.Change change =>
            $"{(context.OnDisk(change.File.Path) ? "changes" : "creates")} {change.File.Path}",
        SetupResult.ByHand hand => $"by hand: {hand.Message}",
        _ => "done",
    };
}
