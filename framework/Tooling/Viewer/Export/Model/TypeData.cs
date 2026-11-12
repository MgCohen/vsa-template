namespace Viewer.Export.Model;

public sealed record TypeData(
    string Id, string Name, string FullName, string Instance, string File, int Line, bool FollowsUser);
