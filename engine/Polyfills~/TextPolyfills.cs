namespace Umpire.Engine.Polyfills;

static class TextPolyfills {
    public static bool StartsWith(this string text, char value) => text.Length > 0 && text[0] == value;

    public static bool EndsWith(this string text, char value) => text.Length > 0 && text[text.Length - 1] == value;

    public static bool Contains(this string text, char value) => text.IndexOf(value) >= 0;

    public static bool Contains(this string text, char value, StringComparison comparison) =>
        text.IndexOf(value.ToString(), comparison) >= 0;

    public static bool Contains(this string text, string value, StringComparison comparison) =>
        text.IndexOf(value, comparison) >= 0;

    public static int IndexOf(this string text, char value, StringComparison comparison) =>
        text.IndexOf(value.ToString(), comparison);

    public static string[] Split(this string text, char separator, StringSplitOptions options) =>
        text.Split([separator], options);

    public static string[] Split(this string text, char separator, int count) => text.Split([separator], count);

    public static string[] Split(this string text, string separator, StringSplitOptions options = StringSplitOptions.None) =>
        text.Split([separator], options);

    public static string Replace(this string text, string oldValue, string? newValue, StringComparison comparison) {
        if (oldValue.Length == 0) {
            throw new ArgumentException("The value to replace is empty.", nameof(oldValue));
        }
        var result = new System.Text.StringBuilder();
        var start = 0;
        for (var found = text.IndexOf(oldValue, comparison); found >= 0; found = text.IndexOf(oldValue, start, comparison)) {
            result.Append(text, start, found - start).Append(newValue);
            start = found + oldValue.Length;
        }
        return result.Append(text, start, text.Length - start).ToString();
    }
}
