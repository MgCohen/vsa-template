namespace Umpire.CheckModel;

using System.Runtime.CompilerServices;

public static class Surface {
    static readonly ConditionalWeakTable<Snapshot, string> Known = new();

    public static string Of(Snapshot snapshot) => Known.GetValue(snapshot, Make);

    public static string Of(TypeFacts type, bool hasFriends) => FileHash.OfText(JsonWriter.Write(Plain(type, hasFriends)));

    static string Make(Snapshot snapshot) => FileHash.OfText(string.Join("|", snapshot.Files
        .SelectMany(f => f.Types)
        .Where(t => IsSeen(t.Content.Access, snapshot.HasFriends))
        .Select(t => Of(t, snapshot.HasFriends))
        .Concat(snapshot.Unplaced.Select(u => u.Surface))
        .OrderBy(text => text, StringComparer.Ordinal)));

    static bool IsSeen(MemberAccess access, bool hasFriends) => access switch {
        MemberAccess.Public or MemberAccess.Protected or MemberAccess.ProtectedInternal => true,
        MemberAccess.Internal or MemberAccess.PrivateProtected => hasFriends,
        _ => false,
    };

    static TypeFacts Plain(TypeFacts type, bool hasFriends) => new(
        type.Name,
        type.Location with { Line = 0, Column = 0 },
        type.Content with {
            IsPartial = false,
            Members = [.. type.Content.Members.Where(m => IsSeen(m.Access, hasFriends))
                .Select(m => m with { Line = 0, IsAsync = false })],
        },
        []);
}
