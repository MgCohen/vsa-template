namespace Umpire.Kernels;

using Umpire.Messages.Contracts;

public interface IOperationConverter {
    IReadOnlyCollection<string> Names { get; }

    bool Accepts(Type message);

    Result<object> Convert(Operation operation);
}
