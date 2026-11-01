using System.Text.Json.Nodes;

namespace VSA.Cli.Scaffolding;

// The root folder of a VSA app: the folder with a global.json that pins VSA.Sdk in msbuild-sdks.
static class AppRoot {
    public static string Find() {
        for (var folder = new DirectoryInfo(Environment.CurrentDirectory); folder is not null; folder = folder.Parent) {
            var file = Path.Combine(folder.FullName, "global.json");
            if (File.Exists(file) && SdkVersion(file) is not null) {
                return folder.FullName;
            }
        }
        throw new VsaException("This folder is not in a VSA app. Run the command in the app folder, or make an app with 'vsa new app <Name>'.");
    }

    // The VSA.Sdk version in global.json, or null.
    public static string? SdkVersion(string globalJson) =>
        (JsonNode.Parse(File.ReadAllText(globalJson)) as JsonObject)?["msbuild-sdks"]?["VSA.Sdk"]?.GetValue<string>();

    // The solution of the app. The tool adds new projects to it with 'dotnet sln add'.
    public static string Solution(string root) =>
        Directory.GetFiles(root, "*.slnx") is [var solution] ? solution
        : throw new VsaException($"The app folder must have one .slnx file: {root}");

    public static void AddToSolution(string root, IEnumerable<string> projects) {
        var (code, output) = Dotnet.Run(root, ["sln", Solution(root), "add", .. projects]);
        if (code != 0) {
            throw new VsaException($"'dotnet sln add' failed. The files are written. Add the projects to the solution by hand.{Environment.NewLine}{output}");
        }
    }
}
