namespace Spike.Core;

public interface IValidator {
    string? Check(object message);
}
