namespace Greeter.Plugins.Tracing;

using Spike.Core;

public sealed class TraceStep : IHubStep {
    public async Task<MessageResult> Run(object message, MessageNext next) {
        await System.Console.Error.WriteLineAsync($"[hub] in: {message}").ConfigureAwait(false);
        var result = await next().ConfigureAwait(false);
        await System.Console.Error.WriteLineAsync($"[hub] out: {(result.IsSuccess ? "ok" : result.Error)}").ConfigureAwait(false);
        return result;
    }
}
