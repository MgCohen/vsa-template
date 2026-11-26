namespace Umpire.Config;

using System.Collections;
using System.Globalization;
using System.Reflection;

static class ConfigBinder {
    static readonly Type[] listTypes = [
        typeof(List<>), typeof(IList<>), typeof(ICollection<>), typeof(IEnumerable<>),
        typeof(IReadOnlyList<>), typeof(IReadOnlyCollection<>)];

    static readonly Type[] dictionaryTypes = [
        typeof(Dictionary<,>), typeof(IDictionary<,>), typeof(IReadOnlyDictionary<,>)];

    public static object Bind(object value, Type type, string path) {
        var target = Nullable.GetUnderlyingType(type) ?? type;
        return value switch {
            ConfigTable table when DictionaryItem(target) is { } item => Dictionary(table, item, path),
            ConfigTable table when IsObject(target) => Object(table, target, path),
            List<object> items when target.IsArray => Array(items, target.GetElementType()!, path),
            List<object> items when ListItem(target) is { } item => List(items, item, path),
            string text when target == typeof(string) => text,
            string text when target.IsEnum => Enumeration(text, target, path),
            bool flag when target == typeof(bool) => flag,
            long number when IsNumber(target) => Number(number, target, path),
            _ => throw Mismatch(value, target, path),
        };
    }

    static object Object(ConfigTable table, Type type, string path) {
        if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) is null) {
            throw new FormatException($"{path} needs {type.Name} with a public constructor that has no parameters.");
        }
        var instance = Activator.CreateInstance(type)!;
        foreach (var pair in table.Entries) {
            if (Property(type, pair.Key) is { } property) {
                property.SetValue(instance, Bind(pair.Value, property.PropertyType, path + "." + pair.Key));
            }
        }
        return instance;
    }

    static object Dictionary(ConfigTable table, Type item, string path) {
        var type = typeof(Dictionary<,>).MakeGenericType(typeof(string), item);
        var dictionary = (IDictionary)Activator.CreateInstance(type)!;
        foreach (var pair in table.Entries) {
            dictionary.Add(pair.Key, Bind(pair.Value, item, path + "." + pair.Key));
        }
        return dictionary;
    }

    static object Array(List<object> items, Type item, string path) {
        var array = System.Array.CreateInstance(item, items.Count);
        for (var i = 0; i < items.Count; i++) {
            array.SetValue(Bind(items[i], item, $"{path}[{i}]"), i);
        }
        return array;
    }

    static object List(List<object> items, Type item, string path) {
        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(item))!;
        for (var i = 0; i < items.Count; i++) {
            list.Add(Bind(items[i], item, $"{path}[{i}]"));
        }
        return list;
    }

    static object Enumeration(string text, Type type, string path) {
        var names = Enum.GetNames(type);
        var name = names.FirstOrDefault(candidate =>
            string.Equals(candidate, text, StringComparison.OrdinalIgnoreCase));
        return name is null
            ? throw new FormatException($"{path} needs one of {string.Join(", ", names)}, and the value is \"{text}\".")
            : Enum.Parse(type, name);
    }

    static object Number(long number, Type type, string path) {
        try {
            return Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
        } catch (OverflowException) {
            throw new FormatException($"{path} needs {type.Name}, and the value {number} is out of its range.");
        }
    }

    static PropertyInfo? Property(Type type, string key) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance).FirstOrDefault(property =>
            property.GetIndexParameters().Length == 0 && property.SetMethod is { IsPublic: true }
                && string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase));

    static Type? ListItem(Type type) =>
        type.IsGenericType && listTypes.Contains(type.GetGenericTypeDefinition())
            ? type.GetGenericArguments()[0]
            : null;

    static Type? DictionaryItem(Type type) =>
        type.IsGenericType && dictionaryTypes.Contains(type.GetGenericTypeDefinition())
            && type.GetGenericArguments()[0] == typeof(string)
            ? type.GetGenericArguments()[1]
            : null;

    static bool IsObject(Type type) =>
        !typeof(IEnumerable).IsAssignableFrom(type)
            && ((type.IsClass && !type.IsAbstract) || (type.IsValueType && !type.IsPrimitive && !IsNumber(type)));

    static bool IsNumber(Type type) =>
        !type.IsEnum && Type.GetTypeCode(type) is >= TypeCode.SByte and <= TypeCode.Decimal;

    static FormatException Mismatch(object value, Type type, string path) =>
        new($"{path} needs {Name(type)}, and the value is {Describe(value)}.");

    static string Name(Type type) =>
        type.IsArray ? $"an array of {type.GetElementType()!.Name}"
            : ListItem(type) is { } item ? $"a list of {item.Name}"
            : DictionaryItem(type) is { } value ? $"a table of {value.Name}"
            : type.Name;

    static string Describe(object value) => value switch {
        string text => $"the string \"{text}\"",
        bool flag => $"the boolean {(flag ? "true" : "false")}",
        long number => $"the integer {number.ToString(CultureInfo.InvariantCulture)}",
        ConfigTable => "a table",
        _ => "an array",
    };
}
