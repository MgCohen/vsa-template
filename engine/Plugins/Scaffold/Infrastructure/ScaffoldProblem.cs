namespace Scaffold.Infrastructure;

public sealed record ScaffoldProblem(ScaffoldProblemKind Kind, string Message);
