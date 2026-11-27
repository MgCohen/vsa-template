namespace CodeCheck.Slices.CheckCode;

using System.Globalization;

sealed class RuleOption {
    readonly Func<string, bool> isValid;

    RuleOption(string key, string kind, string? defaultValue, Func<string, bool> isValid) {
        Key = key;
        Kind = kind;
        Default = defaultValue;
        this.isValid = isValid;
    }

    public string Key { get; }

    public string Kind { get; }

    public string? Default { get; }

    public static RuleOption Number(string key, int? defaultValue = null) =>
        new(key, "a whole number more than 0", defaultValue?.ToString(CultureInfo.InvariantCulture),
            value => ParseInt(value) is not null);

    public static RuleOption Bool(string key, bool? defaultValue = null) =>
        new(key, "true or false", defaultValue is { } value ? (value ? "true" : "false") : null, text => ParseBool(text) is not null);

    public static RuleOption List(string key, IReadOnlyList<string>? defaultValue = null) =>
        new(key, "a list with a comma between the items", defaultValue is null ? null : string.Join(", ", defaultValue), _ => true);

    public bool IsValid(string? value) => (value ?? Default) is { } text && isValid(text);

    internal static int? ParseInt(string? value) => int.TryParse(value, out var number) && number > 0 ? number : null;

    internal static bool? ParseBool(string? value) => bool.TryParse(value, out var flag) ? flag : null;

    internal static IReadOnlyList<string>? ParseList(string? value) =>
        value?.Split(',', ';').Select(item => item.Trim()).Where(item => item.Length > 0).ToArray();
}
