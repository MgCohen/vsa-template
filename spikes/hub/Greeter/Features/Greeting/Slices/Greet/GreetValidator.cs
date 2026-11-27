namespace Greeter.Features.Greeting.Slices.Greet;

using Spike.Core;

public sealed class GreetValidator : Validator<GreetRequest> {
    protected override string? Check(GreetRequest message) =>
        string.IsNullOrWhiteSpace(message.Name) ? "greet: the name is empty." : null;
}
