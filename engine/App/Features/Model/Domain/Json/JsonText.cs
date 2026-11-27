namespace Umpire.CheckModel;

using System.Globalization;
using System.Text;

static class JsonText {
    const int FirstPrintable = 0x20;

    public static void Write(StringBuilder text, string value) {
        text.Append('"');
        foreach (var c in value) {
            text.Append(c switch {
                '"' => "\\\"",
                '\\' => "\\\\",
                '\n' => "\\n",
                '\r' => "\\r",
                '\t' => "\\t",
                _ when c < FirstPrintable => "\\u" + ((int)c).ToString("x4", CultureInfo.InvariantCulture),
                _ => c.ToString(),
            });
        }
        text.Append('"');
    }
}
