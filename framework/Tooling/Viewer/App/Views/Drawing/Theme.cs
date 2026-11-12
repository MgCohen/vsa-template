namespace App.Views.Drawing;

public static class Theme
{
    public const string Edge = "#a3abbc";
    public const string Deny = "#e5484d";
    public const string Unchecked = "#8b93a5";
    public const string Conditional = "#c27c0e";
    public const string Pending = "#2b62d9";

    private static readonly Dictionary<string, string> families = new()
    {
        ["Domain"] = "#7c5cff",
        ["Contracts"] = "#0e8fd0",
        ["Handlers"] = "#e86a10",
        ["Infrastructure"] = "#4d8a0b",
        ["Presenters"] = "#d43c86",
        ["RootClass"] = "#334155",
        ["Views"] = "#0f9d8f",
    };

    public static string LayerColor(string layer)
    {
        return families.GetValueOrDefault(layer.Split('.')[0], "#64748b");
    }

    public static double TagWidth(string text)
    {
        return text.Length * 6.4 + 12;
    }

    public static double BadgeWidth(string text)
    {
        return text.Length * 6.2 + 26;
    }

    public static double TitleWidth(string text)
    {
        return text.Length * 7.8 + 8;
    }
}
