namespace Umpire.Cli.Scaffolding.Setup;

using System.Text;

sealed class SetupPlan(SetupContext context, IReadOnlyList<TaskResult> tasks) {
    public IReadOnlyList<TaskResult> Tasks => tasks;

    public IReadOnlyList<TemplateFile> Files =>
        [.. context.Planned.Select(file => new TemplateFile(file.Key, file.Value))];

    public IReadOnlyList<string> ByHand =>
        [.. tasks.Select(task => task.Result is SetupResult.ByHand hand ? $"{task.Id}: {hand.Message}" : null)
            .OfType<string>()];

    public bool IsComplete => tasks.All(task => task.Result is SetupResult.Done);

    public bool Writes(string path) => context.Planned.ContainsKey(path);

    public void Show() {
        var width = tasks.Max(task => task.Id.Length) + 2;
        foreach (var task in tasks) {
            Console.WriteLine($"  {task.Id.PadRight(width)}{Shown(task.Result)}");
        }
    }

    public void Write() {
        foreach (var (path, content) in context.Planned) {
            var full = context.FullPath(path);
            var exists = File.Exists(full);
            if (exists) {
                var bom = File.ReadAllBytes(full) is [0xEF, 0xBB, 0xBF, ..];
                File.WriteAllText(full, LineEndings.Keep(File.ReadAllText(full), content), new UTF8Encoding(bom));
            } else {
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, content);
            }
            Console.WriteLine($"  {(exists ? "changed" : "created")} {path}");
        }
    }

    string Shown(SetupResult result) => result switch {
        SetupResult.Change change => $"{(context.OnDisk(change.File) ? "changes" : "creates")} {change.File}",
        SetupResult.ByHand hand => $"by hand: {hand.Message}",
        _ => "done",
    };
}
