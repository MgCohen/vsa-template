namespace Umpire.Vsa.Analyzers;

using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public static class ConfigFile {
    static readonly ConditionalWeakTable<AdditionalText, ConfigValues> Parsed = new();

    public static ConfigValues Of(AnalyzerOptions options, CancellationToken cancellation) =>
        File(options) is { } file
            ? Parsed.GetValue(file, text => ConfigValues.Parse(text.GetText(cancellation)?.ToString() ?? ""))
            : ConfigValues.Empty;

    public static AdditionalText? File(AnalyzerOptions options) =>
        options.AdditionalFiles.FirstOrDefault(file =>
            string.Equals(Path.GetFileName(file.Path), ConfigValues.FileName, StringComparison.OrdinalIgnoreCase));
}
