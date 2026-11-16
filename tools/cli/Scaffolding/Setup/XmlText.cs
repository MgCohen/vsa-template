namespace Umpire.Cli.Scaffolding.Setup;

using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

static class XmlText {
    public static XDocument? Parse(string text) {
        try {
            return XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        } catch (XmlException) {
            return null;
        }
    }

    public static string Lines(XElement element, string indent) =>
        indent + element.ToString().Replace("\r\n", "\n").Replace("\n", "\n" + indent) + "\n";

    public static string AddAtStart(string text, string element, string lines) {
        text = Opened(text, element);
        var open = new Regex($@"<{element}\b[^>]*>").Match(text);
        var after = open.Index + open.Length;
        return text.Insert(after, "\n" + lines.TrimEnd('\n'));
    }

    public static string AddAtEnd(string text, string element, string lines) {
        text = Opened(text, element);
        var end = text.LastIndexOf($"</{element}>", StringComparison.Ordinal);
        var lineStart = text.LastIndexOf('\n', Math.Max(end - 1, 0)) + 1;
        return string.IsNullOrWhiteSpace(text[lineStart..end])
            ? text.Insert(lineStart, lines)
            : text.Insert(end, "\n" + lines);
    }

    static string Opened(string text, string element) =>
        new Regex($@"<{element}(\b[^>]*?)\s*/>").Replace(text, $"<{element}$1>\n</{element}>", 1);
}
