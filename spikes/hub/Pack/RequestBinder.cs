namespace Spike.Pack;

using System.Globalization;
using System.Reflection;

sealed class RequestBinder {
    readonly ConstructorInfo constructor;
    readonly ParameterInfo[] parameters;

    public RequestBinder(Type request) {
        constructor = request.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        parameters = constructor.GetParameters();
    }

    public object Bind(IReadOnlyDictionary<string, string> values) =>
        constructor.Invoke([.. parameters.Select(p => Value(p, values))]);

    static object? Value(ParameterInfo parameter, IReadOnlyDictionary<string, string> values) {
        var text = values.FirstOrDefault(v => string.Equals(v.Key, parameter.Name, StringComparison.OrdinalIgnoreCase)).Value;
        if (text is null) {
            return parameter.HasDefaultValue
                ? parameter.DefaultValue
                : throw new ArgumentException($"The value '{parameter.Name}' is missing.");
        }
        return Convert.ChangeType(text, parameter.ParameterType, CultureInfo.InvariantCulture);
    }
}
