namespace Umpire.Vsa.Analyzers;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

static class DomainShared {
    public const string Suffix = ".Domain.Shared";

    public static bool Is(Compilation compilation, AnalyzerOptions options) {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.UmpireLayer", out var layer)
            && layer.Length > 0) {
            return layer == "Domain/Shared";
        }
        return IsAssembly(compilation.AssemblyName);
    }

    public static bool IsAssembly(string? name) => name?.EndsWith(Suffix, System.StringComparison.Ordinal) == true;

    public static string Name(ISymbol member) => member switch {
        IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } m => m.ContainingType.Name,
        _ => member.ToDisplayString(MemberFormat),
    };

    static readonly SymbolDisplayFormat MemberFormat = new(memberOptions: SymbolDisplayMemberOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
