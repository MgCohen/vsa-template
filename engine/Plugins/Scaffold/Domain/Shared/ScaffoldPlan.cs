namespace Scaffold.Domain.Shared;

public sealed record ScaffoldPlan(string Folder, IReadOnlyList<PlannedFile> Files);
