using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSA.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KernelAnalyzer : RuleAnalyzer {
}
