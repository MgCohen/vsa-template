namespace Umpire.CheckModel;

sealed record Exposure(TypeName Target, Access Access, string Member) {
    const string BaseType = "base type";
    const string Constructor = ".ctor";
    const string CompilerName = "<";

    public static IEnumerable<Exposure> Of(AppModel app, AppType type) => All(app, type).Distinct();

    static IEnumerable<Exposure> All(AppModel app, AppType type) {
        var content = type.Content;
        var access = AccessOf(content.Access);
        var bases = new[] { content.Base }.Concat(content.AllInterfaces).OfType<TypeShape>();
        foreach (var target in bases.Where(b => b.Types.Count > 0 && !IsExternal(app, b.Types[0])).SelectMany(b => b.Types)) {
            yield return new Exposure(target, access, BaseType);
        }
        foreach (var member in content.Members.Where(m => !IsCompilerMade(m) && !m.Implements.Any(i => IsExternal(app, i)))) {
            var name = NameOf(member);
            var limit = AccessOf(member.Access) < access ? AccessOf(member.Access) : access;
            foreach (var target in TypesOf(member)) {
                yield return new Exposure(target, limit, name);
            }
        }
    }

    public static IEnumerable<TypeName> TypesOf(MemberFact member) =>
        (member.Type?.Types ?? []).Concat(member.Parameters.SelectMany(p => p.Type.Types));

    static bool IsCompilerMade(MemberFact member) => member.Name.StartsWith(CompilerName, StringComparison.Ordinal);

    static string NameOf(MemberFact member) => member.Kind is MemberKind.Field or MemberKind.Property or MemberKind.Event
        ? member.Name
        : $"{(member.Kind == MemberKind.Constructor ? Constructor : member.Name)}("
            + $"{string.Join(",", member.Parameters.Select(p => p.Passing is null ? p.Type.Text : p.Type.Text + "&"))})";

    static bool IsExternal(AppModel app, TypeName name) => app.TargetOf(name) is { Placement.IsExternal: true };

    static Access AccessOf(MemberAccess access) =>
        access is MemberAccess.Public or MemberAccess.Protected or MemberAccess.ProtectedInternal ? Access.Public : Access.Internal;
}
