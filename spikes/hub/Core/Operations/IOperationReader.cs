namespace Spike.Core;

public interface IOperationReader {
    bool TryRead(Operation operation, out object message);
}
