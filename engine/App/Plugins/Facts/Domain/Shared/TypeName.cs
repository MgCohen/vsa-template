namespace Facts.Domain.Shared;

public sealed record TypeName(string FullName, string Assembly) {
    public const string NoAssembly = "the name has no assembly";

    public string Name => FullName.Split('.')[^1];

    public static TypeName? Parse(string name) {
        var comma = name.IndexOf(',');
        return comma < 0 ? null : new TypeName(name[..comma].Trim(), name[(comma + 1)..].Trim());
    }

    public override string ToString() => $"{FullName}, {Assembly}";
}
