namespace UmpireEngine;

using System.Runtime.CompilerServices;

static class EngineCodes {
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IReadOnlyList<KnownCode> Read() => Task.Run(Codes).GetAwaiter().GetResult();

    [MethodImpl(MethodImplOptions.NoInlining)]
    static IReadOnlyList<KnownCode> Codes() =>
        [.. CompileApp.Codes().Select(code => new KnownCode(code.Code, code.Title, code.AtEnd, code.IsCodeRule))];
}
