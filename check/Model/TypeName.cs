namespace Umpire.CheckModel;

public sealed record TypeName(string FullName, string Assembly) {
    const string Separator = ", ";

    public string Name => FullName.Split('.').Last().Split('+').Last();

    public string Display => FullName.Split('.').Last().Replace('+', '.');

    public string Namespace => FullName.Substring(0, Math.Max(0, FullName.LastIndexOf('.')));

    public static TypeName Parse(string text) {
        var comma = text.LastIndexOf(Separator, StringComparison.Ordinal);
        return comma < 0
            ? throw new FormatException($"\"{text}\" is not a type name with an assembly.")
            : new TypeName(text.Substring(0, comma), text.Substring(comma + Separator.Length));
    }

    public override string ToString() => FullName + Separator + Assembly;
}
