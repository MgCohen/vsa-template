using System.Diagnostics;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace Tests.Shared;

// A copy of a folder in a short temporary folder, prepared as a consumer of the local feed.
// global.json names the VSA.Sdk version, and restore uses its own NuGet cache.
public sealed class Workspace : IDisposable {
    Workspace(string root) {
        Root = root;
        Env = new Dictionary<string, string> { ["NUGET_PACKAGES"] = Path.Combine(root, "nuget") };
    }

    public string Root { get; }

    // The copy of the folder. The consumer files are in Root, one level above.
    public string Src => Path.Combine(Root, "src");

    public IReadOnlyDictionary<string, string> Env { get; }

    public static Workspace Create(string copyFrom) {
        var workspace = new Workspace(TempFolder.Create());
        TempFolder.Copy(copyFrom, workspace.Src);
        workspace.WriteConsumerFiles();
        return workspace;
    }

    public DotnetResult Dotnet(params string[] args) => Shared.Dotnet.Run(Src, Env, args);

    public Process Start(params string[] args) => Shared.Dotnet.Start(Src, Env, args);

    public void Dispose() => TempFolder.Delete(Root);

    void WriteConsumerFiles() {
        var globalJson = JsonNode.Parse(File.ReadAllText(Repo.Path("global.json")))!;
        globalJson["msbuild-sdks"] = new JsonObject { ["VSA.Sdk"] = LocalFeed.Version };
        File.WriteAllText(Path.Combine(Root, "global.json"), globalJson.ToJsonString());

        // The global packages folder of the user is also a feed, so restore does not download each package again.
        var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");
        File.WriteAllText(Path.Combine(Root, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="vsa" value="{LocalFeed.Path}" />
                <add key="cache" value="{globalPackages}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>
            """);

        File.WriteAllText(Path.Combine(Root, "Directory.Build.props"), """
            <Project>
              <PropertyGroup>
                <TargetFramework>net10.0</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
              </PropertyGroup>
            </Project>
            """);

        // The package versions of the repository, and the VSA packages at the test version.
        var packages = XDocument.Load(Repo.Path("Directory.Packages.props"));
        var items = packages.Root!.Element("ItemGroup")!;
        foreach (var id in LocalFeed.Packages.Where(id => id != "VSA.Sdk")) {
            items.Add(new XElement("PackageVersion", new XAttribute("Include", id), new XAttribute("Version", LocalFeed.Version)));
        }
        packages.Save(Path.Combine(Root, "Directory.Packages.props"));
    }
}
