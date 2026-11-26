namespace Umpire.Config;

using System.Globalization;
using System.Text;

static class TomlValues {
    const string True = "true";
    const string False = "false";
    const char DigitSeparator = '_';
    const int MaxCodePoint = 0x10FFFF;
    const int FirstSurrogate = 0xD800;
    const int LastSurrogate = 0xDFFF;

    static readonly char[] exponentMarks = ['e', 'E'];

    public static object Read(TomlCursor cursor) {
        switch (cursor.Current) {
            case '"':
                NotMultiLine(cursor, "\"\"\"");
                return BasicString(cursor);
            case '\'':
                NotMultiLine(cursor, "'''");
                return LiteralString(cursor);
            case '[':
                return Array(cursor);
            case '{':
                return InlineTable(cursor);
            default:
                return Word(cursor);
        }
    }

    public static string BasicString(TomlCursor cursor) {
        cursor.Move();
        var text = new StringBuilder();
        while (cursor.Current != '"') {
            if (cursor.AtLineEnd) {
                throw cursor.Error("The string has no closing quote (\").");
            }
            if (cursor.Current == '\\') {
                text.Append(Escape(cursor));
            } else {
                text.Append(Character(cursor));
            }
        }
        cursor.Move();
        return text.ToString();
    }

    public static string LiteralString(TomlCursor cursor) {
        cursor.Move();
        var text = new StringBuilder();
        while (cursor.Current != '\'') {
            if (cursor.AtLineEnd) {
                throw cursor.Error("The string has no closing quote (').");
            }
            text.Append(Character(cursor));
        }
        cursor.Move();
        return text.ToString();
    }

    static void NotMultiLine(TomlCursor cursor, string quotes) {
        if (cursor.Starts(quotes)) {
            throw cursor.Error($"A multi-line string ({quotes}) is not supported. Write the string on one line.");
        }
    }

    static char Character(TomlCursor cursor) {
        var character = cursor.Current;
        if (TomlCursor.IsControl(character)) {
            throw cursor.Error("A string cannot have a control character. Use an escape, for example \\t.");
        }
        cursor.Move();
        return character;
    }

    static string Escape(TomlCursor cursor) {
        var line = cursor.Line;
        var column = cursor.Column;
        cursor.Move();
        if (cursor.AtLineEnd) {
            throw cursor.Error("The string has no closing quote (\").");
        }
        var code = cursor.Current;
        cursor.Move();
        return code switch {
            'b' => "\b",
            't' => "\t",
            'n' => "\n",
            'f' => "\f",
            'r' => "\r",
            '"' => "\"",
            '\\' => "\\",
            'u' => Unicode(cursor, 4, line, column),
            'U' => Unicode(cursor, 8, line, column),
            _ => throw cursor.Error(line, column, $"The escape \\{code} is not valid. Use \\\\ for a backslash."),
        };
    }

    static string Unicode(TomlCursor cursor, int digits, int line, int column) {
        var hex = new StringBuilder();
        for (var i = 0; i < digits && Uri.IsHexDigit(cursor.Current); i++) {
            hex.Append(cursor.Current);
            cursor.Move();
        }
        var value = hex.Length == digits ? CodePoint(hex.ToString()) : -1;
        if (value < 0 || value > MaxCodePoint || (value >= FirstSurrogate && value <= LastSurrogate)) {
            throw cursor.Error(line, column, $"The escape needs {digits} hex digits of a Unicode scalar value.");
        }
        return char.ConvertFromUtf32(value);
    }

    static int CodePoint(string hex) =>
        int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) ? value : -1;

    static List<object> Array(TomlCursor cursor) {
        cursor.Move();
        var items = new List<object>();
        cursor.SkipBlank();
        while (cursor.Current != ']') {
            if (cursor.AtEnd) {
                throw cursor.Error("The array has no closing bracket (]).");
            }
            if (cursor.Current == '[') {
                throw cursor.Error("An array in an array is not supported.");
            }
            items.Add(Read(cursor));
            cursor.SkipBlank();
            if (cursor.Current == ',') {
                cursor.Move();
                cursor.SkipBlank();
            } else if (cursor.Current != ']') {
                throw cursor.Error(cursor.AtEnd
                    ? "The array has no closing bracket (])."
                    : $"Put a comma (,) between the items of an array. The text has {cursor.Describe()}.");
            }
        }
        cursor.Move();
        return items;
    }

    static ConfigTable InlineTable(TomlCursor cursor) {
        cursor.Move();
        var table = new ConfigTable { FromHeader = true, IsInline = true };
        cursor.SkipSpaces();
        if (cursor.Current == '}') {
            cursor.Move();
            return table;
        }
        while (true) {
            TomlReader.KeyValue(cursor, table);
            cursor.SkipSpaces();
            if (cursor.Current == '}') {
                cursor.Move();
                return table;
            }
            if (cursor.Current != ',') {
                throw cursor.Error(cursor.AtLineEnd
                    ? "The inline table has no closing brace (}). Write an inline table on one line."
                    : $"Put a comma (,) between the keys of an inline table. The text has {cursor.Describe()}.");
            }
            cursor.Move();
            cursor.SkipSpaces();
        }
    }

    static object Word(TomlCursor cursor) {
        var line = cursor.Line;
        var column = cursor.Column;
        var word = new StringBuilder();
        while (TomlReader.IsBareKeyCharacter(cursor.Current) || cursor.Current is '+' or '.' or ':') {
            word.Append(cursor.Current);
            cursor.Move();
        }
        var text = word.ToString();
        if (text.Length == 0) {
            throw cursor.Error(cursor.AtLineEnd || cursor.Current == '#'
                ? "A value is missing. Write a string, a number, a boolean or an array."
                : $"A value cannot start with {cursor.Describe()}.");
        }
        return text switch {
            True => true,
            False => false,
            _ when IsInteger(text) => Integer(cursor, text, line, column),
            _ when IsFloat(text) => Float(cursor, text, line, column),
            _ => throw cursor.Error(line, column, Unsupported(text)),
        };
    }

    static long Integer(TomlCursor cursor, string text, int line, int column) {
        if (!long.TryParse(WithoutSeparators(text), NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out var value)) {
            throw cursor.Error(line, column, $"The integer {text} is out of range.");
        }
        return value;
    }

    static ConfigFloat Float(TomlCursor cursor, string text, int line, int column) {
        var plain = WithoutSeparators(text);
        var unsigned = plain.TrimStart('+', '-');
        var negative = plain[0] == '-';
        if (unsigned is "inf" or "nan") {
            var special = unsigned == "nan" ? double.NaN : negative ? double.NegativeInfinity : double.PositiveInfinity;
            return new ConfigFloat(special, plain);
        }
        var isValid = double.TryParse(plain, NumberStyles.Float, CultureInfo.InvariantCulture, out var value);
        if (!isValid || double.IsInfinity(value)) {
            throw cursor.Error(line, column, $"The float {text} is out of range.");
        }
        return new ConfigFloat(value, plain);
    }

    static string WithoutSeparators(string text) => text.Replace(DigitSeparator.ToString(), "");

    static bool IsFloat(string text) {
        var unsigned = text[0] is '+' or '-' ? text.Substring(1) : text;
        if (unsigned is "inf" or "nan") {
            return true;
        }
        var exponent = unsigned.IndexOfAny(exponentMarks);
        var mantissa = exponent < 0 ? unsigned : unsigned.Substring(0, exponent);
        var point = mantissa.IndexOf('.');
        var whole = point < 0 ? mantissa : mantissa.Substring(0, point);
        var hasFraction = point >= 0 && IsDigits(mantissa.Substring(point + 1));
        var hasExponent = exponent >= 0 && IsExponent(unsigned.Substring(exponent + 1));
        var hasMark = point >= 0 || exponent >= 0;
        return hasMark && IsInteger(whole) && (point < 0 || hasFraction) && (exponent < 0 || hasExponent);
    }

    static bool IsExponent(string text) =>
        text.Length > 0 && IsDigits(text[0] is '+' or '-' ? text.Substring(1) : text);

    static bool IsDigits(string text) {
        if (text.Length == 0) {
            return false;
        }
        for (var i = 0; i < text.Length; i++) {
            var isDigit = text[i] >= '0' && text[i] <= '9';
            var isSeparator = text[i] == DigitSeparator && i > 0 && i < text.Length - 1
                && text[i + 1] != DigitSeparator;
            if (!isDigit && !isSeparator) {
                return false;
            }
        }
        return true;
    }

    static bool IsInteger(string text) {
        var digits = text.Length > 0 && text[0] is '+' or '-' ? text.Substring(1) : text;
        return IsDigits(digits) && (digits.Length == 1 || digits[0] != '0');
    }

    static string Unsupported(string text) {
        var lower = text.ToLowerInvariant();
        if (lower.StartsWith("0x", StringComparison.Ordinal) || lower.StartsWith("0o", StringComparison.Ordinal)
            || lower.StartsWith("0b", StringComparison.Ordinal)) {
            return $"The integer {text} is not supported. Write a decimal integer.";
        }
        if (text.IndexOf(':') >= 0 || (text.Length > 4 && text[4] == '-' && char.IsDigit(text[0]))) {
            return $"The date or time {text} is not supported. Write it as a string in quotes.";
        }
        var unsigned = lower.TrimStart('+', '-');
        var isNumber = unsigned.Length > 0 && char.IsDigit(unsigned[0]);
        if (isNumber && (unsigned.IndexOf('.') >= 0 || unsigned.IndexOf('e') >= 0)) {
            return $"The float {text} is not valid. Write digits on the two sides of '.', for example 0.5 or 1e3.";
        }
        return $"The value {text} is not valid. A string needs quotes, for example \"{text}\".";
    }
}
