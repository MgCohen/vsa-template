namespace UmpireEngine;

using Facts.Domain.Shared.Compile;
using Jobs.Slices.CompileJob;
using Operations.Domain.Shared;
using Umpire.Vsa.Composition;

static class CompileApp {
    static readonly Lazy<CompileHost> Running = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static CompileJobReport Send(CompileJobRequest request, CancellationToken cancellation) {
        var result = Running.Value.SendAsync(request, cancellation).GetAwaiter().GetResult();
        return result.IsSuccess ? result.Value : throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
    }

    public static IReadOnlyList<FindingCode> Codes() =>
        Send(new CompileJobRequest(CompileProject.None, CompileStage.Load), CancellationToken.None).Codes;

    public static bool IsLoadFailure(Exception e) => e switch {
        FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException or MissingMemberException => true,
        TypeInitializationException { InnerException: { } inner } => IsLoadFailure(inner),
        _ => false,
    };

    public static bool IsFailure(Exception e) => e is not OperationCanceledException;

    public static string Reason(Exception e) =>
        e is TypeInitializationException { InnerException: { } inner } ? Reason(inner) : e.Message;

    static CompileHost Start() {
        var host = new CompileHost();
        new EngineApp().Run(host);
        return host;
    }
}
