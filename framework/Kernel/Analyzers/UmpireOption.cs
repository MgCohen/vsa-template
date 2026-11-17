namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Generic;

public sealed class UmpireOption {
    readonly Func<string, bool> isValid;

    UmpireOption(string key, string kind, string? defaultValue, Func<string, bool> isValid) {
        Key = key;
        Kind = kind;
        Default = defaultValue;
        this.isValid = isValid;
    }

    public string Key { get; }

    public string Kind { get; }

    public string? Default { get; }

    public static UmpireOption Int(string key, int? defaultValue = null) =>
        new(key, "a whole number more than 0", defaultValue?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            value => int.TryParse(value, out var number) && number > 0);

    public static UmpireOption Bool(string key, bool? defaultValue = null) =>
        new(key, "true or false", defaultValue is { } value ? (value ? "true" : "false") : null, text => bool.TryParse(text, out _));

    public static UmpireOption List(string key, IReadOnlyList<string>? defaultValue = null) =>
        new(key, "a list with a comma between the items", defaultValue is null ? null : string.Join(", ", defaultValue), _ => true);

    public bool IsValid(string? value) => (value ?? Default) is { } text && isValid(text);
}
