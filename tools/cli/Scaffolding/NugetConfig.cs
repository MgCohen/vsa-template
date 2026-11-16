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
        UmpireFeed(document).ReplaceWith(SourceFeeds(sources, []));
        document.Descendants(Credentials).Remove();
        return $"{document.Declaration}\n{document}\n".Replace("\r\n", "\n");
    }

    public static bool HasFeeds(XDocument config, IReadOnlyList<string> sources) {
        var feeds = Feeds(config);
        return sources.Count == 0
            ? feeds.Any(feed => (string?)feed.Attribute("key") == UmpireFeedKey)
            : sources.All(source => Values(feeds).Contains(Path.GetFullPath(source)));
    }

    public static string? AddFeeds(string config, XDocument document, string template, IReadOnlyList<string> sources) {
        var unit = XmlText.Indent(config, Root);
        var templateDocument = XDocument.Parse(template);
        var existing = Feeds(document);
        List<XElement> feeds = sources.Count == 0
            ? [UmpireFeed(templateDocument)]
            : SourceFeeds(
                [.. sources.Where(source => !Values(existing).Contains(Path.GetFullPath(source)))],
                existing.Select(feed => (string?)feed.Attribute("key")).OfType<string>().ToHashSet());
        var feedLines = string.Concat(feeds.Select(feed => XmlText.Lines(feed, unit + unit, unit)));
        var withFeeds = document.Descendants(Sources).Any()
            ? XmlText.AddAtEnd(config, Sources, feedLines)
            : XmlText.AddAtEnd(config, Root, $"{unit}<{Sources}>\n{feedLines}{unit}</{Sources}>\n");
        if (withFeeds is null || sources.Count > 0
            || document.Descendants(Credentials).Elements(UmpireFeedKey).Any()) {
            return withFeeds;
        }
        var umpireCredential = templateDocument.Descendants(Credentials).Elements().Single();
        var credential = XmlText.Lines(umpireCredential, unit + unit, unit);
        return document.Descendants(Credentials).Any()
            ? XmlText.AddAtEnd(withFeeds, Credentials, credential)
            : XmlText.AddAtEnd(withFeeds, Root, $"{unit}<{Credentials}>\n{credential}{unit}</{Credentials}>\n");
    }

    static List<XElement> Feeds(XDocument config) => [.. config.Descendants(Sources).Elements("add")];

    static HashSet<string> Values(IEnumerable<XElement> feeds) =>
        feeds.Select(feed => (string?)feed.Attribute("value"))
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    static XElement UmpireFeed(XDocument document) =>
        document.Descendants("add")
            .Single(e => (string?)e.Attribute("key") == UmpireFeedKey && e.Parent?.Name == Sources);

    static List<XElement> SourceFeeds(IReadOnlyList<string> sources, HashSet<string> usedKeys) {
        var keys = Enumerable.Range(1, sources.Count + usedKeys.Count)
            .Select(i => i == 1 ? UmpireFeedKey : $"{UmpireFeedKey}{i}")
            .Where(key => !usedKeys.Contains(key));
        return [.. sources.Zip(keys, (source, key) => new XElement(
            "add",
            new XAttribute("key", key),
            new XAttribute("value", Path.GetFullPath(source))))];
    }
}
