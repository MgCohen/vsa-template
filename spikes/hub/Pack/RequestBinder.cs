namespace Spike.Pack;

using System.Globalization;
using System.Linq.Expressions;

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
        values = [.. parameters.Select((p, i) => new RequestValue(p.Name!, i.ToString(CultureInfo.InvariantCulture), p.ParameterType, p.HasDefaultValue, p.DefaultValue))];
    }

    public object Bind(IReadOnlyDictionary<string, string> input) =>
        create([.. values.Select(v => v.Read(input))]);

    sealed record RequestValue(string Name, string Position, Type Type, bool HasDefault, object? Default) {
        public object? Read(IReadOnlyDictionary<string, string> input) =>
            input.TryGetValue(Name, out var text) || input.TryGetValue(Position, out text)
                ? Convert.ChangeType(text, Type, CultureInfo.InvariantCulture)
            : HasDefault ? Default
            : throw new ArgumentException($"The value '{Name}' is missing.");
    }
}
