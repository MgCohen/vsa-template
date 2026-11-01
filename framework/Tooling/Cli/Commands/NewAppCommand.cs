using System.Security;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa new app <Name>: the root files with the version of this tool, App/Host, App/Views and an empty Modules/ folder
// (only Modules/.gitkeep, so that git keeps the folder).
static class NewAppCommand {
    // The .NET SDK of a new app. VSA.Sdk targets net10.0.
    const string DotnetSdk = "10.0.100";

    // sources: extra package feeds for nuget.config, for example a local folder with the VSA packages.
    public static int Run(string name, IReadOnlyList<string> sources) {
        Names.Check(name, "app");
        var root = Path.GetFullPath(name);
        var version = ToolVersion.Current;
        var files = new List<(string Path, string Content)> {
            ("global.json", GlobalJson(version)),
            ("dotnet-tools.json", ToolsJson(version)),
            ("nuget.config", NugetConfig(sources)),
            ($"{name}.slnx", Solution),
        };
        files.AddRange(Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = name }, version));
        Console.WriteLine($"Making the app {name} (VSA {version}):");
        FileWriter.Write(root, files);

        var (code, output) = Dotnet.Run(root, "tool", "restore");
        if (code != 0) {
            Console.Error.WriteLine($"vsa: 'dotnet tool restore' failed. Run it in {name} before 'dotnet vsa'.{Environment.NewLine}{output}");
        }
        Console.WriteLine($"""

            Next:
              cd {name}
              dotnet vsa add module <Name>
            """);
        return 0;
    }

    static string GlobalJson(string version) => $$"""
        {
          "sdk": {
            "version": "{{DotnetSdk}}",
            "rollForward": "latestFeature"
          },
          "msbuild-sdks": {
            "VSA.Sdk": "{{version}}"
          }
        }

        """;

    static string ToolsJson(string version) => $$"""
        {
          "version": 1,
          "isRoot": true,
          "tools": {
            "vsa.cli": {
              "version": "{{version}}",
              "commands": [
                "vsa"
              ],
              "rollForward": false
            }
          }
        }

        """;

    static string NugetConfig(IReadOnlyList<string> sources) {
        var local = string.Concat(sources.Select((source, i) =>
            $"    <add key=\"vsa{(i == 0 ? "" : i + 1)}\" value=\"{SecurityElement.Escape(Path.GetFullPath(source))}\" />\n"));
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
            {local}    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
            </configuration>

            """;
    }

    const string Solution = """
        <Solution>
          <Folder Name="/App/">
            <Project Path="App/Host/App.Host.csproj" />
            <Project Path="App/Views/App.Views.csproj" />
          </Folder>
        </Solution>

        """;
}
