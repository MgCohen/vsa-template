namespace Umpire.Cli.Scaffolding.Setup;

using System.Text;

static class LineEndings {
    const string Crlf = "\r\n";
    const string Lf = "\n";

    public static string Keep(string original, string content) {
        var lines = Lines(original);
        var crlf = lines.Count(line => line.Ending == Crlf);
        var main = crlf > lines.Count(line => line.Ending == Lf) ? Crlf : Lf;
        var parts = content.Split('\n');
        var builder = new StringBuilder();
        var next = 0;
        for (var i = 0; i < parts.Length - 1; i++) {
            var ending = main;
            if (next < lines.Count && lines[next].Text == parts[i]) {
                ending = lines[next].Ending ?? main;
                next++;
            }
            builder.Append(parts[i]).Append(ending);
        }
        return builder.Append(parts[^1]).ToString();
    }

    static List<Line> Lines(string text) {
        var parts = text.Split('\n');
        var lines = new List<Line>();
        for (var i = 0; i < parts.Length; i++) {
            var last = i == parts.Length - 1;
            if (last && parts[i] is "") {
                break;
            }
            var part = parts[i];
            lines.Add(last ? new Line(part, null)
                : part.EndsWith('\r') ? new Line(part[..^1], Crlf)
                : new Line(part, Lf));
        }
        return lines;
    }

    sealed record Line(string Text, string? Ending);
}
