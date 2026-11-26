namespace Greeter.Features.Greeting;

public sealed class PoliteFormat : IGreetingFormat {
    public string Format(string name) => $"Hello, {name}.";
}
