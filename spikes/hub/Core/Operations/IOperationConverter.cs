namespace Spike.Core;

public interface IOperationConverter {
    MessageResult Convert(Operation operation);
}
