namespace Umpire.Config;

sealed class TomlCursor {
    const char End = '\0';

    readonly string text;
    readonly string source;
    int position;

    public TomlCursor(string text, string source) {
        this.text = text;
        this.source = source;
    }

    public bool AtEnd => position >= text.Length;

    public char Current => AtEnd ? End : text[position];

    public bool AtLineEnd => AtEnd || Current == '\n' || (Current == '\r' && Following == '\n');

    public int Line { get; private set; } = 1;

    public int Column { get; private set; } = 1;

    char Following => position + 1 < text.Length ? text[position + 1] : End;

    public static bool IsControl(char character) => (character < ' ' && character != '\t') || character == '\u007f';

    public void Move() {
        if (Current == '\n') {
            Line++;
            Column = 1;
        } else {
            Column++;
        }
        position++;
    }

    public bool Starts(string word) =>
        position + word.Length <= text.Length && string.CompareOrdinal(text, position, word, 0, word.Length) == 0;

    public void SkipSpaces() {
        while (Current == ' ' || Current == '\t') {
            Move();
        }
    }

    public void SkipBlank() {
        while (true) {
            SkipSpaces();
            SkipComment();
            if (AtEnd || !AtLineEnd) {
                return;
            }
            NewLine();
        }
    }

    public void EndLine() {
        SkipSpaces();
        SkipComment();
        if (AtEnd) {
            return;
        }
        if (!AtLineEnd) {
            throw Error($"The line has more text after its end: {Describe()}. Put each key on its own line.");
        }
        NewLine();
    }

    public void Expect(char character, string what) {
        SkipSpaces();
        if (Current != character) {
            throw Error($"Write '{character}' {what}. The text has {Describe()}.");
        }
        Move();
    }

    public string Describe() => AtLineEnd ? "the end of the line" : $"'{Current}'";

    public ConfigFormatException Error(string reason) => Error(Line, Column, reason);

    public ConfigFormatException Error(int line, int column, string reason) => new(source, line, column, reason);

    void SkipComment() {
        if (Current != '#') {
            return;
        }
        while (!AtLineEnd) {
            if (IsControl(Current)) {
                throw Error("A comment cannot have a control character.");
            }
            Move();
        }
    }

    void NewLine() {
        if (Current == '\r') {
            Move();
        }
        Move();
    }
}
