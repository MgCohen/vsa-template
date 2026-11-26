namespace Greeter.Features.Greeting.Slices.Greet;

public sealed record GreetResponse(string Text) {
    public override string ToString() => Text;
}
