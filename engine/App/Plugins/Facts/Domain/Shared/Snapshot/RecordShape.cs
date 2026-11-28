namespace Umpire.CheckModel;

using System.Collections.Concurrent;
using System.Reflection;

sealed class RecordShape {
    static readonly ConcurrentDictionary<Type, RecordShape> Shapes = new();

    readonly ConstructorInfo constructor;
    readonly IReadOnlyList<RecordField> fields;
    readonly HashSet<string> keys;

    RecordShape(Type type) {
        constructor = type.GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
        fields = [.. constructor.GetParameters().Select(p => new RecordField(type.GetProperty(p.Name!)!, p))];
        keys = [.. fields.Select(f => f.Key)];
    }

    public static RecordShape Of(Type type) => Shapes.GetOrAdd(type, t => new RecordShape(t));

    public bool Has(string key) => keys.Contains(key);

    public IEnumerable<RecordField> Written(object record) => fields.Where(field => field.IsWritten(record));

    public object Make(Func<RecordField, object?> value) {
        var values = new object?[fields.Count];
        for (var i = 0; i < values.Length; i++) {
            values[i] = value(fields[i]);
        }
        return constructor.Invoke(values);
    }
}
