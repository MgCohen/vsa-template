namespace Spike.Core;

public sealed record Operation(string Name, IReadOnlyDictionary<string, string> Values);

public interface IOperationConverter {
    MessageResult Convert(Operation operation);
}
