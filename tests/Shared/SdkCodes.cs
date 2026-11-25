namespace Tests.Shared;

using System.Xml.Linq;

public static class SdkCodes {
    public static string File { get; } = Repo.Path("tools", "sdk", "Sdk", "Umpire.Codes.props");

    public static IReadOnlyList<string> All { get; } =
        [.. XDocument.Load(File).Descendants("UmpireCodes").First().Value
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(code => !code.StartsWith('$'))];
}
