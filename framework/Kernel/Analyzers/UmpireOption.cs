namespace Umpire.Vsa.Analyzers;

using System;

public sealed class UmpireOption {
    readonly Func<string, bool> isValid;

    UmpireOption(string key, string kind, Func<string, bool> isValid) {
        Key = key;
        Kind = kind;
        this.isValid = isValid;
    }

    public string Key { get; }

    public string Kind { get; }

    public static UmpireOption Int(string key) =>
        new(key, "a whole number more than 0", value => int.TryParse(value, out var number) && number > 0);

    public static UmpireOption Bool(string key) => new(key, "true or false", value => bool.TryParse(value, out _));

    public static UmpireOption List(string key) => new(key, "a list with a comma between the items", _ => true);

    public bool IsValid(string? value) => value is not null && isValid(value);
}
