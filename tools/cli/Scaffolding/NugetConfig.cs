namespace Umpire.Cli.Scaffolding;

using System.Xml.Linq;

static class NugetConfig {
    public const string FileName = "nuget.config";
    const string UmpireFeedKey = "umpire";

    public static string Write(string template, IReadOnlyList<string> sources) {
        if (sources.Count == 0) {
            return template;
        }
        var document = XDocument.Parse(template);
        var vsaFeed = document.Descendants("add")
            .Single(e => (string?)e.Attribute("key") == UmpireFeedKey && e.Parent?.Name == "packageSources");
        vsaFeed.ReplaceWith(sources.Select((source, i) => new XElement(
            "add",
            new XAttribute("key", i == 0 ? UmpireFeedKey : $"{UmpireFeedKey}{i + 1}"),
            new XAttribute("value", Path.GetFullPath(source)))));
        document.Descendants("packageSourceCredentials").Remove();
        return $"{document.Declaration}\n{document}\n".Replace("\r\n", "\n");
    }
}
