namespace Umpire.Vsa.Analyzers;

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

public abstract class UmpireAnalyzerRule {
    const string Area = "01";

    DiagnosticDescriptor? rule;

    public abstract int Group { get; }

    public abstract int Number { get; }

    public string Id => $"UMP{Area}{Group:00}{Number:00}";

    public DiagnosticDescriptor Rule => rule ??= new(Id, Title, Message, "Umpire", Severity, isEnabledByDefault: true);

    public abstract DiagnosticSeverity Severity { get; }

    public virtual bool IsModuleRule => true;

    public virtual IReadOnlyList<UmpireOption> Options => [];

    protected abstract string Title { get; }

    protected abstract string Message { get; }

    protected internal abstract void Register(CompilationStartAnalysisContext start);

    protected UmpireOptions Values(AnalyzerOptions options, SyntaxTree tree) =>
        new(Id, options.AnalyzerConfigOptionsProvider.GetOptions(tree), Options);

    protected Diagnostic Create(Location location, params object[] arguments) => Diagnostic.Create(Rule, location, arguments);
}
