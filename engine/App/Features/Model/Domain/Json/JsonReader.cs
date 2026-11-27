namespace Umpire.CheckModel;

static class JsonReader {
    public static T Read<T>(string text) where T : class =>
        (T)(Value(typeof(T), JsonParser.Parse(text), typeof(T).Name) ?? throw new FormatException($"The text has no {typeof(T).Name}."));

    static object? Value(Type type, object? value, string place) {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        if (value is null) {
            return null;
        }
        return Plain(target, value) ?? Structured(target, value, place)
            ?? throw new FormatException($"{place} is not a {target.Name}.");
    }

    static object? Plain(Type target, object value) {
        if (target == typeof(int)) {
            return value is long number and >= int.MinValue and <= int.MaxValue ? (int)number : null;
        }
        if (target == typeof(TypeName)) {
            return value is string typeName ? TypeName.Parse(typeName) : null;
        }
        if (target.IsEnum) {
            return value is string name && Enum.IsDefined(target, name) ? Enum.Parse(target, name) : null;
        }
        return target.IsInstanceOfType(value) ? value : null;
    }

    static object? Structured(Type target, object value, string place) {
        if (target == typeof(IReadOnlyDictionary<string, string>)) {
            return value is Dictionary<string, object?> pairs && pairs.Values.All(v => v is string)
                ? pairs.ToDictionary(p => p.Key, p => (string)p.Value!)
                : null;
        }
        if (RecordField.ItemOf(target) is { } item) {
            return value is List<object?> list ? Items(item, list, place) : null;
        }
        return value is Dictionary<string, object?> fields && IsRecord(target) ? Record(target, fields, place) : null;
    }

    static bool IsRecord(Type type) => type.IsClass && type.Namespace == typeof(JsonReader).Namespace && type != typeof(TypeName);

    static Array Items(Type item, List<object?> list, string place) {
        var values = Array.CreateInstance(item, list.Count);
        for (var i = 0; i < list.Count; i++) {
            values.SetValue(Value(item, list[i] ?? throw new FormatException($"{place} has an empty item."), place), i);
        }
        return values;
    }

    static object Record(Type type, Dictionary<string, object?> fields, string place) {
        var shape = RecordShape.Of(type);
        if (fields.Keys.FirstOrDefault(key => !shape.Has(key)) is { } unknown) {
            throw new FormatException($"{place} has the field {unknown}, but {type.Name} does not have it.");
        }
        return shape.Make(field => fields.TryGetValue(field.Key, out var value) && value is not null
            ? Value(field.Property.PropertyType, value, $"{type.Name}.{field.Key}")
            : field.Missing ?? (field.IsOptional ? null : throw new FormatException($"{place} has no field {field.Key}.")));
    }
}
