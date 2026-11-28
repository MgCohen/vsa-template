namespace Umpire.CheckModel;

using System.Reflection;

sealed class RecordField {
    public RecordField(PropertyInfo property, ParameterInfo parameter) {
        Property = property;
        Key = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
        Missing = MissingOf(property.PropertyType);
        IsOptional = parameter.HasDefaultValue;
    }

    public PropertyInfo Property { get; }
    public string Key { get; }
    public object? Missing { get; }
    public bool IsOptional { get; }

    public static Type? ItemOf(Type type) =>
        type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IReadOnlyList<>) ? type.GetGenericArguments()[0] : null;

    public bool IsWritten(object record) => Property.GetValue(record) switch {
        null => false,
        bool value => value,
        string => true,
        System.Collections.IEnumerable list => list.GetEnumerator().MoveNext(),
        _ => true,
    };

    static object? MissingOf(Type type) {
        if (type == typeof(bool)) {
            return false;
        }
        if (type == typeof(IReadOnlyDictionary<string, string>)) {
            return new Dictionary<string, string>();
        }
        return ItemOf(type) is { } item ? Array.CreateInstance(item, 0) : null;
    }
}
