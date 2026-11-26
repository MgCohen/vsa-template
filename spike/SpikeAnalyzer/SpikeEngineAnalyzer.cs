namespace SpikeAnalyzer {
    using System.Collections.Immutable;
    using System.Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.Diagnostics;
    using SpikeApp;
    using SpikeApp.Features.Describe.Slices.DescribeType;
    using Umpire.Kernels;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SpikeEngineAnalyzer : DiagnosticAnalyzer {
        static readonly DiagnosticDescriptor Rule = new(
            "SPK001", "Engine response", "{0} (compose {1} ms in process {2}, send {3} ms)", "Spike",
            DiagnosticSeverity.Warning, isEnabledByDefault: true);

        static readonly DiagnosticDescriptor Failure = new(
            "SPK002", "Engine failure", "{0}", "Spike", DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule, Failure];

        public override void Initialize(AnalysisContext context) {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        }

        static void Analyze(SymbolAnalysisContext context) {
            var location = context.Symbol.Locations.FirstOrDefault() ?? Location.None;
            try {
                Run(context, location);
            } catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(Failure, location, e.ToString().Replace("\n", " | ")));
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        static void Run(SymbolAnalysisContext context, Location location) {
            var type = (INamedTypeSymbol)context.Symbol;
            var app = EngineHolder.App.Value;
            var watch = Stopwatch.StartNew();
            var result = app.Send(new DescribeTypeRequest(type.Name, type.GetMembers().Length));
            var sendMs = watch.Elapsed.TotalMilliseconds;
            var message = result.Value is DescribeTypeResponse response ? response.Message : $"error {result.Error}";
            context.ReportDiagnostic(Diagnostic.Create(Rule, location, message, EngineHolder.composeMs.ToString("F1"),
                Process.GetCurrentProcess().Id, sendMs.ToString("F3")));
        }
    }

    static class EngineHolder {
        internal static double composeMs;

        internal static readonly Lazy<SpikeApp<LocalHost>> App = new(() => {
            var watch = Stopwatch.StartNew();
            var app = new SpikeApp<LocalHost>();
            app.Start([]);
            composeMs = watch.Elapsed.TotalMilliseconds;
            return app;
        });

    }
}
