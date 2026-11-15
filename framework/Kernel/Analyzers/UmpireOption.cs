using System;

namespace Umpire.Vsa.Analyzers;

// One option that a rule needs. The value is only in Umpire.Rules.globalconfig, or in a consumer .editorconfig: no rule has a default.
// VsaAnalyzer checks each option at the start of a compilation. If the option is missing or not valid, it reports UMP1000,
// and the rule does not run.
public sealed class UmpireOption {
    readonly Func<string, bool> isValid;

    UmpireOption(string key, string kind, Func<string, bool> isValid) {
        Key = key;
        Kind = kind;
        this.isValid = isValid;
    }

    public string Key { get; }

    // The kind of value, for the message of UMP1000: "a whole number", "true or false", "a list".
    public string Kind { get; }

    // A whole number that is more than 0, for example umpire.UMP1008.max = 120.
    public static UmpireOption Int(string key) =>
        new(key, "a whole number more than 0", value => int.TryParse(value, out var number) && number > 0);

    public static UmpireOption Bool(string key) => new(key, "true or false", value => bool.TryParse(value, out _));

    // A list, with a comma between the items. An empty value is an empty list.
    public static UmpireOption List(string key) => new(key, "a list with a comma between the items", _ => true);

    public bool IsValid(string? value) => value is not null && isValid(value);
}
