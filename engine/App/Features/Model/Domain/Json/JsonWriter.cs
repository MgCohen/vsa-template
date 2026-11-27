namespace Umpire.CheckModel;

using System.Collections;
using System.Globalization;
using System.Text;

sealed class JsonWriter {
    const int LineDepth = 4;
    const string Step = "  ";

    readonly StringBuilder text = new();
    int depth;

    public static string Write(object value) {
        var writer = new JsonWriter();
        writer.Value(value);
        return writer.text.Append('\n').ToString();
    }

    void Value(object value) {
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
                Items('{', '}', pairs.OrderBy(p => p.Key, StringComparer.Ordinal), pair => Field(pair.Key, pair.Value));
                break;
            case IEnumerable list:
                Items('[', ']', list.Cast<object>(), Value);
                break;
            default:
                Items('{', '}', RecordShape.Of(value.GetType()).Written(value), field => Field(field.Key, field.Property.GetValue(value)!));
                break;
        }
    }

    void Field(string key, object value) {
        JsonText.Write(text, key);
        text.Append(':');
        Value(value);
    }

    void Items<T>(char open, char close, IEnumerable<T> items, Action<T> write) {
        text.Append(open);
        var start = text.Length;
        var lines = depth < LineDepth;
        depth++;
        foreach (var item in items) {
            if (text.Length > start) {
                text.Append(',');
            }
            if (lines) {
                Line();
            }
            write(item);
        }
        depth--;
        if (lines && text.Length > start) {
            Line();
        }
        text.Append(close);
    }

    void Line() => text.Append('\n').Insert(text.Length, Step, depth);
}
