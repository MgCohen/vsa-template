namespace Spike.Pack;

using System.Globalization;
using System.Reflection;
using Spike.Core;

sealed class RequestConverter(IReadOnlyDictionary<string, Type> requests) : IOperationConverter {
    readonly Dictionary<string, ConstructorInfo> constructors = requests.ToDictionary(
        r => r.Key, r => r.Value.GetConstructors().Single(), StringComparer.OrdinalIgnoreCase);

    public MessageResult Convert(Operation operation) {
        if (!constructors.TryGetValue(operation.Name, out var constructor)) {
            return MessageResult.Fail($"No request has the name '{operation.Name}'.");
        }
        var parameters = constructor.GetParameters();
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++) {
            if (!operation.Values.TryGetValue(parameters[i].Name!, out var text) && !operation.Values.TryGetValue($"{i}", out text)) {
                return MessageResult.Fail($"{operation.Name}: the value '{parameters[i].Name}' is missing.");
            }
            try {
                arguments[i] = System.Convert.ChangeType(text, parameters[i].ParameterType, CultureInfo.InvariantCulture);
            } catch (Exception e) when (e is FormatException or OverflowException) {
                return MessageResult.Fail($"{operation.Name}: '{text}' is not a valid {parameters[i].ParameterType.Name}.");
            }
        }
        return MessageResult.Ok(constructor.Invoke(arguments));
    }
}
