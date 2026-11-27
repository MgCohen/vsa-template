namespace Spike.Pack;

using System.Globalization;
using System.Linq.Expressions;
using Spike.Core;

sealed class RequestBinder {
    readonly Func<object?[], object> create;
    readonly RequestValue[] values;

    public RequestBinder(Type request) {
        var constructor = request.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        var parameters = constructor.GetParameters();
        var input = Expression.Parameter(typeof(object?[]));
        var arguments = parameters.Select((p, i) =>
            Expression.Convert(Expression.ArrayIndex(input, Expression.Constant(i)), p.ParameterType));
        create = Expression.Lambda<Func<object?[], object>>(
            Expression.Convert(Expression.New(constructor, arguments), typeof(object)), input).Compile();
        values = [.. parameters.Select((p, i) => new RequestValue(
            p.Name!, i.ToString(CultureInfo.InvariantCulture), p.ParameterType, p.HasDefaultValue, DefaultOf(p)))];
    }

    public MessageResult Bind(Operation operation) {
        var arguments = new object?[values.Length];
        for (var i = 0; i < values.Length; i++) {
            if (values[i].Read(operation.Values, out var value) is { } error) {
                return MessageResult.Fail($"{operation.Name}: {error}");
            }
            arguments[i] = value;
        }
        return MessageResult.Ok(create(arguments));
    }

    static object? DefaultOf(System.Reflection.ParameterInfo parameter) =>
        parameter.HasDefaultValue && parameter.DefaultValue is not null ? parameter.DefaultValue
        : parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType)
        : null;

    sealed record RequestValue(string Name, string Position, Type Type, bool HasDefault, object? Default) {
        public string? Read(IReadOnlyDictionary<string, string> input, out object? value) {
            value = Default;
            if (!input.TryGetValue(Name, out var text) && !input.TryGetValue(Position, out text)) {
                return HasDefault ? null : $"the value '{Name}' is missing.";
            }
            try {
                value = System.Convert.ChangeType(text, Type, CultureInfo.InvariantCulture);
                return null;
            } catch (Exception e) when (e is FormatException or InvalidCastException or OverflowException) {
                return $"the value '{Name}' is not a valid {Type.Name}: '{text}'.";
            }
        }
    }
}
