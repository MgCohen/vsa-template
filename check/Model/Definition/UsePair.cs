namespace Umpire.CheckModel;

public sealed record UsePair(int From, int To, Access Access, IReadOnlyList<int> Same, IReadOnlyList<int> Other);
