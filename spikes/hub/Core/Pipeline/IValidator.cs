namespace Spike.Core;

public interface IValidator {
    Type Message { get; }

    string? Check(object message);
}
