namespace Spike.Core;

public interface IOperationConverter {
    bool TryConvert(Operation operation, out object message);
}
