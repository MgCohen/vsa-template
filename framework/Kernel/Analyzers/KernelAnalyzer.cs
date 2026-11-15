using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Umpire.Vsa.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class KernelAnalyzer : RuleAnalyzer {
}
