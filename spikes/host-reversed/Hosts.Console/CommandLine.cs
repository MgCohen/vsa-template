namespace Spike.Hosts.Console;

public sealed class CommandLine(string[] args) {
    public string? Word => args.FirstOrDefault(arg => !arg.StartsWith("--", StringComparison.Ordinal));

    public string? Option(string name) {
        var index = Array.FindIndex(args, arg => string.Equals(arg, "--" + name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
