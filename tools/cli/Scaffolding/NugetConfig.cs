namespace Umpire.Cli.Scaffolding;

using System.Xml.Linq;
using Umpire.Cli.Scaffolding.Setup;

static class NugetConfig {
    public const string FileName = "nuget.config";
    const string UmpireFeedKey = "umpire";
    const string Sources = "packageSources";
    const string Credentials = "packageSourceCredentials";
    const string Root = "configuration";

    public static string Write(string template, IReadOnlyList<string> sources) {
        if (sources.Count == 0) {
            return template;
        }
        var document = XDocument.Parse(template);
        UmpireFeed(document).ReplaceWith(SourceFeeds(sources));
        document.Descendants(Credentials).Remove();
        return $"{document.Declaration}\n{document}\n".Replace("\r\n", "\n");
    }

    public static bool HasUmpireFeed(XDocument config) =>
        config.Descendants(Sources).Elements("add").Any(e => (string?)e.Attribute("key") == UmpireFeedKey);

    public static string AddFeed(string config, XDocument document, string template, IReadOnlyList<string> sources) {
        var templateDocument = XDocument.Parse(template);
        var feeds = sources.Count == 0 ? [UmpireFeed(templateDocument)] : SourceFeeds(sources);
        var feedLines = string.Concat(feeds.Select(feed => XmlText.Lines(feed, "    ")));
        config = document.Descendants(Sources).Any()
            ? XmlText.AddAtEnd(config, Sources, feedLines)
            : XmlText.AddAtEnd(config, Root, $"  <{Sources}>\n{feedLines}  </{Sources}>\n");
        if (sources.Count > 0) {
            return config;
        }
        var credential = XmlText.Lines(templateDocument.Descendants(Credentials).Elements().Single(), "    ");
        return document.Descendants(Credentials).Any()
            ? XmlText.AddAtEnd(config, Credentials, credential)
            : XmlText.AddAtEnd(config, Root, $"  <{Credentials}>\n{credential}  </{Credentials}>\n");
    }

    static XElement UmpireFeed(XDocument document) =>
        document.Descendants("add")
            .Single(e => (string?)e.Attribute("key") == UmpireFeedKey && e.Parent?.Name == Sources);

    static List<XElement> SourceFeeds(IReadOnlyList<string> sources) =>
        [.. sources.Select((source, i) => new XElement(
            "add",
            new XAttribute("key", i == 0 ? UmpireFeedKey : $"{UmpireFeedKey}{i + 1}"),
            new XAttribute("value", Path.GetFullPath(source))))];
}
