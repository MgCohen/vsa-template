namespace UmpireEngine;

using Jobs.Slices.CompileJob;
using Umpire.Vsa.Composition;

static class CompileApp {
    static readonly Lazy<EngineApp<CompileHost>> Running = new(Start, LazyThreadSafetyMode.ExecutionAndPublication);

    public static CompileJobReport Send(CompileJobRequest request, CancellationToken cancellation) {
        var result = Running.Value.SendAsync(request, cancellation).GetAwaiter().GetResult();
        return result.IsSuccess ? result.Value : throw new InvalidOperationException($"{result.Error!.Code}: {result.Error.Message}");
    }

    public static bool IsLoadFailure(Exception e) => e switch {
        FileNotFoundException or FileLoadException or BadImageFormatException or TypeLoadException or MissingMemberException => true,
        TypeInitializationException { InnerException: { } inner } => IsLoadFailure(inner),
        _ => false,
    };

    public static bool IsFailure(Exception e) => e is not OperationCanceledException;

    public static string Reason(Exception e) =>
        e is TypeInitializationException { InnerException: { } inner } ? Reason(inner) : e.Message;

    static EngineApp<CompileHost> Start() {
        var app = new EngineApp<CompileHost>();
        app.Start([]);
        return app;
    }
}
