namespace Umpire.CheckModel;

using System.Globalization;
using System.Text;

sealed class JsonParser {
    const int HexDigits = 4;
    const int MaxDepth = 256;

    readonly string text;
    int at;
    int depth;

    JsonParser(string text) {
        this.text = text;
    }

    public static object? Parse(string text) {
        var parser = new JsonParser(text);
        var value = parser.Value();
        parser.Space();
        return parser.at == text.Length ? value : throw parser.Bad("the end of the text");
    }

    object? Value() {
        Space();
        return Peek() switch {
            '{' => Object(),
            '[' => Array(),
            '"' => String(),
            't' => Word("true", true),
            'f' => Word("false", false),
            'n' => Word("null", null),
            _ => Number(),
        };
    }

    Dictionary<string, object?> Object() {
        Enter();
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        for (var more = !Skip('}'); more; more = HasMore('}')) {
            Space();
            var key = String();
            Expect(':');
            values[key] = Value();
        }
        depth--;
        return values;
    }

    List<object?> Array() {
        Enter();
        var values = new List<object?>();
        for (var more = !Skip(']'); more; more = HasMore(']')) {
            values.Add(Value());
        }
        depth--;
        return values;
    }

    void Enter() {
        at++;
        if (++depth > MaxDepth) {
            throw Bad("less nesting");
        }
    }

    string String() {
        if (Peek() != '"') {
            throw Bad("a string");
        }
        var start = ++at;
        while (at < text.Length && text[at] != '"' && text[at] != '\\') {
            at++;
        }
        if (Peek() == '"') {
            return text.Substring(start, at++ - start);
        }
        var value = new StringBuilder(text, start, at - start, Math.Max(16, (at - start) * 2));
        while (Peek() != '"') {
            value.Append(text[at] == '\\' ? Escape() : text[at++]);
        }
        at++;
        return value.ToString();
    }

    char Escape() {
        at++;
        var c = Peek();
        at++;
        return c switch {
            'n' => '\n',
            'r' => '\r',
            't' => '\t',
            'b' => '\b',
            'f' => '\f',
            'u' => Unicode(),
            _ => c,
        };
    }

    char Unicode() {
        if (at + HexDigits > text.Length
            || !int.TryParse(text.Substring(at, HexDigits), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var code)) {
            throw Bad("four hex digits");
        }
        at += HexDigits;
        return (char)code;
    }

    object Number() {
        var start = at;
        while (at < text.Length && (char.IsDigit(text[at]) || text[at] == '-')) {
            at++;
        }
        return long.TryParse(text.Substring(start, at - start), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)
            ? number
            : throw Bad("a whole number");
    }

    object? Word(string word, object? value) {
        if (string.CompareOrdinal(text, at, word, 0, word.Length) != 0) {
            throw Bad(word);
        }
        at += word.Length;
        return value;
    }

    bool HasMore(char close) {
        if (Skip(',')) {
            return true;
        }
        Expect(close);
        return false;
    }

    bool Skip(char c) {
        Space();
        if (at >= text.Length || text[at] != c) {
            return false;
        }
        at++;
        return true;
    }

    void Expect(char c) {
        if (!Skip(c)) {
            throw Bad(c.ToString());
        }
    }

    void Space() {
        while (at < text.Length && char.IsWhiteSpace(text[at])) {
            at++;
        }
    }

    char Peek() => at < text.Length ? text[at] : throw Bad("more text");

    FormatException Bad(string expected) => new($"The JSON text is not valid at character {at}: {expected} is missing.");
}
