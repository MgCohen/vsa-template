namespace Umpire.Config;

using System.Text;

static class TomlReader {
    const char ByteOrderMark = '\uFEFF';
    const char Dot = '.';

    public static ConfigTable Read(string text, string source) {
        var cursor = new TomlCursor(text, source);
        var root = new ConfigTable();
        var current = root;
        if (cursor.Current == ByteOrderMark) {
            cursor.Move();
        }
        while (!cursor.AtEnd) {
            cursor.SkipSpaces();
            if (cursor.Current == '[') {
                current = Header(cursor, root);
            } else if (!cursor.AtLineEnd && cursor.Current != '#') {
                KeyValue(cursor, current);
            }
            cursor.EndLine();
        }
        return root;
    }

    public static bool IsBareKeyCharacter(char character) =>
        (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z')
            || (character >= '0' && character <= '9') || character == '_' || character == '-';

    public static void KeyValue(TomlCursor cursor, ConfigTable table) {
        var line = cursor.Line;
        var column = cursor.Column;
        var keys = Keys(cursor);
        var name = string.Join(Dot.ToString(), keys);
        cursor.Expect('=', $"after the key {name}");
        cursor.SkipSpaces();
        var value = TomlValues.Read(cursor);
        var target = table;
        for (var i = 0; i < keys.Count - 1; i++) {
            target = Dotted(cursor, target, keys[i], line, column, name);
        }
        var last = keys[keys.Count - 1];
        if (Existing(cursor, target, last, line, column) is not null) {
            throw cursor.Error(line, column, $"The key {name} is defined two times. Write each key one time.");
        }
        target.Add(last, value);
    }

    static ConfigTable Header(TomlCursor cursor, ConfigTable root) {
        var line = cursor.Line;
        var column = cursor.Column;
        cursor.Move();
        if (cursor.Current == '[') {
            throw cursor.Error(line, column, "An array of tables ([[name]]) is not supported. Use a table [name].");
        }
        var keys = Keys(cursor);
        cursor.Expect(']', "at the end of the table header");
        var name = string.Join(Dot.ToString(), keys);
        var table = root;
        for (var i = 0; i < keys.Count - 1; i++) {
            table = Inner(cursor, table, keys[i], line, column, name);
        }
        var last = keys[keys.Count - 1];
        if (Existing(cursor, table, last, line, column) is { } existing) {
            if (existing is not ConfigTable found) {
                throw cursor.Error(line, column, $"The key {name} has a value, so it cannot also be a table.");
            }
            if (found.FromHeader || found.FromDottedKey) {
                throw cursor.Error(line, column,
                    $"The table [{name}] is defined two times. Write each table one time.");
            }
            found.FromHeader = true;
            return found;
        }
        var created = new ConfigTable { FromHeader = true };
        table.Add(last, created);
        return created;
    }

    static ConfigTable Inner(TomlCursor cursor, ConfigTable table, string key, int line, int column, string name) {
        switch (Existing(cursor, table, key, line, column)) {
            case ConfigTable { IsInline: false } found:
                return found;
            case ConfigTable:
                throw cursor.Error(line, column,
                    $"The table [{name}] cannot add a key to the inline table {key}. "
                        + "Write all its keys inside its braces.");
            case null:
                var created = new ConfigTable();
                table.Add(key, created);
                return created;
            default:
                throw cursor.Error(line, column,
                    $"The key {key} of [{name}] has a value, so it cannot also be a table.");
        }
    }

    static ConfigTable Dotted(TomlCursor cursor, ConfigTable table, string key, int line, int column, string name) {
        switch (Existing(cursor, table, key, line, column)) {
            case ConfigTable { FromHeader: false } found:
                return found;
            case null:
                var created = new ConfigTable { FromDottedKey = true };
                table.Add(key, created);
                return created;
            default:
                throw cursor.Error(line, column,
                    $"The key {name} cannot add a value to {key}: {key} is already defined.");
        }
    }

    static object? Existing(TomlCursor cursor, ConfigTable table, string key, int line, int column) {
        if (table.Spelling(key) is { } spelling && !string.Equals(spelling, key, StringComparison.Ordinal)) {
            throw cursor.Error(line, column,
                $"The key {key} differs from the key {spelling} only in case. "
                    + "Umpire reads keys with no case, so use one spelling.");
        }
        return table.Find(key);
    }

    static List<string> Keys(TomlCursor cursor) {
        var keys = new List<string>();
        while (true) {
            cursor.SkipSpaces();
            keys.Add(Key(cursor));
            cursor.SkipSpaces();
            if (cursor.Current != Dot) {
                return keys;
            }
            cursor.Move();
        }
    }

    static string Key(TomlCursor cursor) {
        if (cursor.Current == '"') {
            return TomlValues.BasicString(cursor);
        }
        if (cursor.Current == '\'') {
            return TomlValues.LiteralString(cursor);
        }
        var key = new StringBuilder();
        while (IsBareKeyCharacter(cursor.Current)) {
            key.Append(cursor.Current);
            cursor.Move();
        }
        if (key.Length == 0) {
            throw cursor.Error($"A key cannot start with {cursor.Describe()}. "
                + "Use letters, digits, '_' and '-' in a key, or put the key in quotes.");
        }
        return key.ToString();
    }
}
