namespace Steps.Infrastructure;

public sealed class RunOptions {
    public bool FailFast { get; set; }

    public IReadOnlyList<string> References { get; set; } = [];

    public string? SourceRoot { get; set; }

    public IReadOnlyList<string> IgnoredFolders { get; set; } = [];

    public bool ProjectChecks { get; set; } = true;
}
