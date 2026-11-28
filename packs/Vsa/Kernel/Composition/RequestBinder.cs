namespace Umpire.Vsa.Composition;

using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Umpire.Messages.Contracts;

sealed class RequestBinder {
    public const string NotValidCode = "request.not_valid";

    readonly Type request;
    readonly ConstructorInfo constructor;
    readonly ParameterInfo[] parameters;

    public RequestBinder(Type request) {
        this.request = request;
        constructor = request.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        parameters = constructor.GetParameters();
    }

    public Result<object> Bind(IReadOnlyDictionary<string, string?> values) {
        var arguments = new object?[parameters.Length];
        for (var i = 0; i < parameters.Length; i++) {
            var parameter = parameters[i];
            var found = values.Where(v => string.Equals(v.Key, parameter.Name, StringComparison.OrdinalIgnoreCase)).ToList();
            if (found.Count > 0 && found[0].Value is null) {
                if (parameter.ParameterType.IsValueType && Nullable.GetUnderlyingType(parameter.ParameterType) is null) {
                    return Invalid($"The value '{parameter.Name}' of {request.Name} must not be null.");
                }
                arguments[i] = null;
                continue;
            }
            var text = found.Count > 0 ? found[0].Value : null;
            if (text is null) {
                if (!parameter.HasDefaultValue) {
                    return Invalid($"{request.Name} needs the value '{parameter.Name}'.");
                }
                arguments[i] = parameter.DefaultValue;
                continue;
            }
            try {
                arguments[i] = Value(parameter, text);
            } catch (Exception e) when (e is FormatException or ArgumentException or NotSupportedException or OverflowException
                || e.InnerException is FormatException or OverflowException) {
                return Invalid($"The value '{parameter.Name}' of {request.Name} is not valid: {e.Message}");
            }
        }
        try {
            return constructor.Invoke(arguments);
        } catch (TargetInvocationException e) when (e.InnerException is ArgumentException or FormatException) {
            return Invalid($"{request.Name}: {e.InnerException.Message}");
        }
    }

    static Result<object> Invalid(string message) => new Error(NotValidCode, message);

    static object? Value(ParameterInfo parameter, string text) {
        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
        if (type.IsEnum) {
            return EnumValue(type, parameter.Name!, text);
        }
        if (type == typeof(DateTime)) {
            return DateTime.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }
        var converter = TypeDescriptor.GetConverter(type);
        if (!converter.CanConvertFrom(typeof(string))) {
            throw new NotSupportedException(
                $"An operation gives text values, and {type.Name} has no conversion from text. Give the request simple values.");
        }
        return converter.ConvertFromInvariantString(text);
    }

    static object EnumValue(Type type, string name, string text) {
        var names = Enum.GetNames(type);
        string[] parts = type.IsDefined(typeof(FlagsAttribute), false)
            ? [.. text.Split(',').Select(part => part.Trim()).Where(part => part.Length > 0)]
            : [text];
        List<string?> found = [.. parts.Select(part => (string?)names.FirstOrDefault(n => string.Equals(n, part, StringComparison.OrdinalIgnoreCase)))];
        if (found.Count == 0 || found.Contains(null)) {
            throw new FormatException($"'{name}' must be one of {string.Join(", ", names)}. The value is '{text}'.");
        }
        var value = found.Aggregate(0L, (all, member) => all | System.Convert.ToInt64(Enum.Parse(type, member!), CultureInfo.InvariantCulture));
        return Enum.ToObject(type, value);
    }
}
