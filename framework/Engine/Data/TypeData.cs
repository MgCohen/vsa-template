namespace VSA.Architecture.Data;

public sealed record TypeData(
    string Id, string Name, string FullName, string Instance, string File, int Line, bool FollowsUser);
