namespace Facts.Domain.Shared;

public sealed record TypeName(string FullName, string Assembly) {
    public const string NoAssembly = "the name has no assembly";

    public string Name => FullName.Split('.').Last();

    public static TypeName? Parse(string name) {
        var comma = name.IndexOf(',');
        return comma < 0 ? null : new TypeName(name.Substring(0, comma).Trim(), name.Substring(comma + 1).Trim());
    }

    public override string ToString() => $"{FullName}, {Assembly}";
}
