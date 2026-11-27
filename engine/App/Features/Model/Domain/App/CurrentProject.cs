namespace Umpire.CheckModel;

public sealed record CurrentProject(Snapshot Snapshot, Func<TypeName, IReadOnlyList<AppReference>> References);
