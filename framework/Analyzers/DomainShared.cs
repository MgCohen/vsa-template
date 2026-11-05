using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

// The test of the Domain.Shared rules (VSA1005, VSA1006, VSA1007). A <Module>.Domain.Shared project is the published
// domain of a module: the keys, ids and components that other modules read.
// VSA.Sdk gives the layer in the MSBuild property VsaLayer. If the property is not there, the assembly name decides.
static class DomainShared {
    public const string Suffix = ".Domain.Shared";

    public static bool Is(Compilation compilation, AnalyzerOptions options) {
        if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue("build_property.VsaLayer", out var layer)
            && layer.Length > 0) {
            return layer == "Domain/Shared";
        }
        return IsAssembly(compilation.AssemblyName);
    }

    public static bool IsAssembly(string? name) => name?.EndsWith(Suffix, System.StringComparison.Ordinal) == true;

    // The name of a member in a message, for example Name, operator + or implicit operator int.
    public static string Name(ISymbol member) => member switch {
        IMethodSymbol { MethodKind: MethodKind.Constructor or MethodKind.StaticConstructor } m => m.ContainingType.Name,
        _ => member.ToDisplayString(MemberFormat),
    };

    static readonly SymbolDisplayFormat MemberFormat = new(memberOptions: SymbolDisplayMemberOptions.None,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);
}
