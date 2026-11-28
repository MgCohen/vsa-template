namespace Umpire.CheckModel;

using System.Collections;
using System.Globalization;
using System.Text;

sealed class JsonWriter {
    const int LineDepth = 4;
    const string Step = "  ";

    readonly StringBuilder text = new();

    public static string Write(object value) {
        var writer = new JsonWriter();
        writer.Value(value, 0);
        return writer.text.Append('\n').ToString();
    }

    void Value(object value, int depth) {
        switch (value) {
            case string s:
                JsonText.Write(text, s);
                break;
            case bool b:
                text.Append(b ? "true" : "false");
                break;
            case int or long:
                text.Append(((IFormattable)value).ToString(null, CultureInfo.InvariantCulture));
                break;
            case Enum or TypeName:
                JsonText.Write(text, value.ToString()!);
                break;
            case IReadOnlyDictionary<string, string> pairs:
                Items('{', '}', pairs.OrderBy(p => p.Key, StringComparer.Ordinal), depth, pair => Field(pair.Key, pair.Value, depth + 1));
                break;
            case IEnumerable list:
                Items('[', ']', list.Cast<object>(), depth, item => Value(item, depth + 1));
                break;
            default:
                Items('{', '}', RecordShape.Of(value.GetType()).Written(value), depth,
                    field => Field(field.Key, field.Property.GetValue(value)!, depth + 1));
                break;
        }
    }

    void Field(string key, object value, int depth) {
        JsonText.Write(text, key);
        text.Append(':');
        Value(value, depth);
    }

    void Items<T>(char open, char close, IEnumerable<T> items, int depth, Action<T> write) {
        text.Append(open);
        var start = text.Length;
        var lines = depth < LineDepth;
        foreach (var item in items) {
            if (text.Length > start) {
                text.Append(',');
            }
            if (lines) {
                Line(depth + 1);
            }
            write(item);
        }
        if (lines && text.Length > start) {
            Line(depth);
        }
        text.Append(close);
    }

    void Line(int depth) => text.Append('\n').Insert(text.Length, Step, depth);
}
