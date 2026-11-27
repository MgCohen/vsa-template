namespace Operations.Infrastructure;

using Operations.Domain.Shared;

public sealed class RunOptions {
    public bool FailFast { get; set; }

    public IReadOnlyList<string> References { get; set; } = [];

    public string? SourceRoot { get; set; }

    public IReadOnlyList<string> IgnoredFolders { get; set; } = [];

    public bool ProjectChecks { get; set; } = true;

    public void Apply(CheckSettings settings) {
        References = settings.References;
        SourceRoot = settings.SourceRoot;
        IgnoredFolders = settings.IgnoredFolders;
        ProjectChecks = settings.ProjectChecks;
    }

    public string Inputs(ReportMode? mode, string skip, string block) =>
        $"{mode}|{skip}|{block}|{string.Join(";", References)}|{SourceRoot}"
        + $"|{string.Join(";", IgnoredFolders)}|{ProjectChecks}";
}
