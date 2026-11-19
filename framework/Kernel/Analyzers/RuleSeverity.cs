namespace Umpire.Vsa.Analyzers;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Umpire.Config;

public static class RuleSeverity {
    public const string Key = "severity";

    static readonly ConditionalWeakTable<SyntaxTree, ConcurrentDictionary<string, DiagnosticSeverity>> Trees = new();

    public static string? Text(UmpireConfig config, string ruleId) => config.Value($"umpire.{ruleId}.{Key}");

    public static bool IsOff(string? text) => Normal(text) is "none" or "silent";

    public static DiagnosticSeverity? Of(string? text) => Normal(text) switch {
        "error" => DiagnosticSeverity.Error,
        "warning" => DiagnosticSeverity.Warning,
        "suggestion" or "info" => DiagnosticSeverity.Info,
        _ => null,
    };

    internal static void Use(Compilation compilation, UmpireConfig config, IEnumerable<UmpireAnalyzerRule> rules) {
        foreach (var tree in compilation.SyntaxTrees) {
            var severities = Trees.GetValue(tree, _ => new ConcurrentDictionary<string, DiagnosticSeverity>(StringComparer.Ordinal));
            foreach (var rule in rules) {
                if (Of(Text(config, rule.Id)) is { } severity) {
                    severities[rule.Id] = severity;
                } else {
                    severities.TryRemove(rule.Id, out _);
                }
            }
        }
    }

    internal static DiagnosticSeverity? For(Location location, string ruleId) =>
        location.SourceTree is { } tree && Trees.TryGetValue(tree, out var severities)
            && severities.TryGetValue(ruleId, out var severity) ? severity : null;

    static string? Normal(string? text) => text?.Trim().ToLowerInvariant();
}
