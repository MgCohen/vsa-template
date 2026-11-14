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
}
