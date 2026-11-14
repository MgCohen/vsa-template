using System.Security;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa new app <Name>: the root files with the version of this tool, the root project, App/Host, App/Views, Tests and an empty Features/ folder
// (only Features/.gitkeep, so that git keeps the folder).
static class NewAppCommand {
    // The .NET SDK of a new app. VSA.Sdk targets net10.0.
    const string DotnetSdk = "10.0.100";

    // sources: the package feeds of the VSA packages for nuget.config, for example a local folder. Default: the GitHub feed.
    public static int Run(string name, IReadOnlyList<string> sources) {
        Names.Check(name, "app");
        var root = Path.GetFullPath(name);
        var version = ToolVersion.Current;
        var files = new List<(string Path, string Content)> {
            ("global.json", GlobalJson(version)),
            ("dotnet-tools.json", ToolsJson(version)),
            ("nuget.config", NugetConfig(sources)),
            (".gitignore", GitIgnore),
            ($"{name}.slnx", Solution.Replace(Templates.App, name, StringComparison.Ordinal)),
        };
        files.AddRange(Templates.Files(Templates.Kind.App, new Dictionary<string, string> { [Templates.App] = name }, version));
        Console.WriteLine($"Making the app {name} (VSA {version}):");
        FileWriter.Write(root, files);

        var (code, output) = Dotnet.Run(root, "tool", "restore");
        if (code != 0) {
            throw new VsaException($"The app is written, but 'dotnet tool restore' failed. Fix the error, then run it in {name}.{Environment.NewLine}{output}");
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
          },
          "test": {
            "runner": "Microsoft.Testing.Platform"
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

    internal const string GitIgnore = """
        bin/
        obj/
        TestResults/
        *.user
        .vs/

        """;

    // The feed of the published VSA packages. GitHub Packages needs a token also for public packages:
    // the GITHUB_TOKEN of a workflow, or `gh auth token` with the read:packages scope on a dev machine.
    internal const string GitHubFeed = "https://nuget.pkg.github.com/MgCohen/index.json";

    // With no sources, the VSA packages come from GitHubFeed. Each source replaces it, for example a local folder.
    internal static string NugetConfig(IReadOnlyList<string> sources) {
        if (sources.Count == 0) {
            return $"""
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <packageSources>
                    <clear />
                    <add key="vsa" value="{GitHubFeed}" />
                    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                  </packageSources>
                  <packageSourceCredentials>
                    <vsa>
                      <add key="Username" value="vsa" />
                      <add key="ClearTextPassword" value="%GITHUB_TOKEN%" />
                    </vsa>
                  </packageSourceCredentials>
                </configuration>

                """;
        }
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
          <Project Path="SampleApp.csproj" />
          <Folder Name="/Tests/">
            <Project Path="Tests/App.Tests.csproj" />
          </Folder>
        </Solution>

        """;
}
