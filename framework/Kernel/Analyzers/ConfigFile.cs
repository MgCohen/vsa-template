namespace Umpire.Vsa.Analyzers;

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Umpire.Config;

public static class ConfigFile {
    static readonly ConditionalWeakTable<AdditionalText, UmpireConfig> Parsed = new();

    public static UmpireConfig Of(AnalyzerOptions options, CancellationToken cancellation) =>
        File(options) is { } file
            ? Parsed.GetValue(file, text => UmpireConfig.Parse(text.GetText(cancellation)?.ToString() ?? ""))
            : UmpireConfig.Empty;

    public static AdditionalText? File(AnalyzerOptions options) =>
        options.AdditionalFiles.FirstOrDefault(file =>
            string.Equals(Path.GetFileName(file.Path), UmpireConfig.FileName, StringComparison.OrdinalIgnoreCase));
}
