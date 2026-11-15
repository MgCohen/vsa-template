using System.Xml.Linq;

namespace VSA.Cli.Scaffolding;

static class NugetConfig {
    public const string FileName = "nuget.config";
    const string VsaFeedKey = "vsa";

    public static string Write(string template, IReadOnlyList<string> sources) {
        if (sources.Count == 0) {
            return template;
        }
        var document = XDocument.Parse(template);
        var vsaFeed = document.Descendants("add")
            .Single(e => (string?)e.Attribute("key") == VsaFeedKey && e.Parent?.Name == "packageSources");
        vsaFeed.ReplaceWith(sources.Select((source, i) => new XElement(
            "add",
            new XAttribute("key", i == 0 ? VsaFeedKey : $"{VsaFeedKey}{i + 1}"),
            new XAttribute("value", Path.GetFullPath(source)))));
        document.Descendants("packageSourceCredentials").Remove();
        return $"{document.Declaration}\n{document}\n".Replace("\r\n", "\n");
    }
}
