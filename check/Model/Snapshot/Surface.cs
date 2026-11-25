namespace Umpire.CheckModel;

using System.Runtime.CompilerServices;

public static class Surface {
    static readonly ConditionalWeakTable<Snapshot, string> Known = new();

    public static string Of(Snapshot snapshot) => Known.GetValue(snapshot, Make);

    public static string Of(TypeFacts type, bool hasFriends) => Of([type], hasFriends);

    static string Make(Snapshot snapshot) => FileHash.OfText(string.Join("|", snapshot.Files
        .SelectMany(f => f.Types)
        .Where(t => IsSeen(t.Content.Access, snapshot.HasFriends))
        .GroupBy(t => t.Name)
        .Select(parts => Of([.. parts], snapshot.HasFriends))
        .Concat(snapshot.Unplaced.Select(u => u.Surface))
        .OrderBy(text => text, StringComparer.Ordinal)));

    static string Of(IReadOnlyList<TypeFacts> parts, bool hasFriends) {
        var members = parts.SelectMany(p => p.Content.Members).Where(m => IsSeen(m.Access, hasFriends)).ToList();
        var names = new HashSet<string>(members.Select(m => m.Name));
        var attributes = parts.SelectMany(p => p.Content.Attributes).Where(a => a.Member is null || names.Contains(a.Member));
        var type = new TypeFacts(
            parts[0].Name,
            parts[0].Location with { Line = 0, Column = 0 },
            parts[0].Content with { IsPartial = false, MutableMembers = [], Members = [], Attributes = [] },
            []);
        var texts = members.Select(m => JsonWriter.Write(Plain(m, hasFriends))).Concat(attributes.Select(JsonWriter.Write));
        return FileHash.OfText(JsonWriter.Write(type) + string.Join("|", texts.OrderBy(text => text, StringComparer.Ordinal)));
    }

    static MemberFact Plain(MemberFact member, bool hasFriends) => member with {
        Line = 0,
        IsAsync = false,
        IsReadOnly = member.Kind != MemberKind.Property && member.IsReadOnly,
        Get = Seen(member.Get, hasFriends),
        Set = Seen(member.Set, hasFriends),
    };

    static MemberAccess? Seen(MemberAccess? access, bool hasFriends) =>
        access is { } given && IsSeen(given, hasFriends) ? given : null;

    static bool IsSeen(MemberAccess access, bool hasFriends) => access switch {
        MemberAccess.Public or MemberAccess.Protected or MemberAccess.ProtectedInternal => true,
        MemberAccess.Internal or MemberAccess.PrivateProtected => hasFriends,
        _ => false,
    };
}
