namespace Umpire.Cli.Scaffolding.Setup;

using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

static class XmlText {
    const string DefaultIndent = "  ";
    static readonly Regex ChildIndent = new(@"\G\s*?\n([ \t]+)<");

    public static XDocument? Parse(string text) {
        try {
            return XDocument.Parse(text, LoadOptions.PreserveWhitespace);
        } catch (XmlException) {
            return null;
        }
    }

    public static string Indent(string text, string element) {
        var open = OpenTag(element).Match(text);
        if (!open.Success) {
            return DefaultIndent;
        }
        var child = ChildIndent.Match(text, open.Index + open.Length);
        return child.Success ? child.Groups[1].Value : DefaultIndent;
    }

    public static string Lines(XElement element, string indent, string unit) {
        var settings = new XmlWriterSettings {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = unit,
            NewLineChars = "\n",
            ConformanceLevel = ConformanceLevel.Fragment,
        };
        var builder = new StringBuilder();
        using (var writer = XmlWriter.Create(builder, settings)) {
            element.WriteTo(writer);
        }
        return indent + builder.ToString().Replace("\n", "\n" + indent) + "\n";
    }

    public static string? AddAtStart(string text, string element, string lines) {
        text = Opened(text, element);
        var open = OpenTag(element).Match(text);
        return open.Success ? text.Insert(open.Index + open.Length, "\n" + lines.TrimEnd('\n')) : null;
    }

    public static string? AddAtEnd(string text, string element, string lines) {
        text = Opened(text, element);
        var end = text.LastIndexOf($"</{element}>", StringComparison.Ordinal);
        if (end < 0) {
            return null;
        }
        var lineStart = end == 0 ? 0 : text.LastIndexOf('\n', end - 1) + 1;
        return string.IsNullOrWhiteSpace(text[lineStart..end])
            ? text.Insert(lineStart, lines)
            : text.Insert(end, "\n" + lines);
    }

    static Regex OpenTag(string element) => new($@"<{element}\b[^>]*(?<!/)>");

    static string Opened(string text, string element) {
        var closed = new Regex($@"<{element}(\b[^>]*?)\s*/>").Match(text);
        if (!closed.Success) {
            return text;
        }
        var lineStart = closed.Index == 0 ? 0 : text.LastIndexOf('\n', closed.Index - 1) + 1;
        var before = text[lineStart..closed.Index];
        var indent = string.IsNullOrWhiteSpace(before) ? before : "";
        var opened = $"<{element}{closed.Groups[1].Value}>\n{indent}</{element}>";
        return text[..closed.Index] + opened + text[(closed.Index + closed.Length)..];
    }
}
