namespace Spike.Core;

sealed class ConsoleAppLog : IAppLog {
    public void Warning(string text) => System.Console.Error.WriteLine($"warning: {text}");

    public void Error(string text) => System.Console.Error.WriteLine($"error: {text}");
}
