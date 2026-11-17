namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using System.Text.Json.Nodes;
using Umpire.Engine.Versions;
using Umpire.Files;

sealed class GlobalJsonTask : SetupTask {
    const string FileName = "global.json";
    const string SdksKey = "msbuild-sdks";
    const string TestKey = "test";
    static readonly string[] KeptParentKeys = ["sdk", SdksKey, TestKey];

    public override string Id => "global-json";


    public override SetupResult Plan(SetupContext context) {
        if (context.Read(FileName) is not string text) {
            return new SetupResult.Change(new TextFile(FileName, New(context)));
        }
        if (JsonText.Parse(text) is not JsonObject json) {
            return new SetupResult.ByHand($"{FileName} has comments or is not valid JSON. "
                + $"Make sure that it has no \"{SdksKey}\" entry for {VersionSlots.Sdk}.");
        }
        return json[SdksKey]?[VersionSlots.Sdk] is null
            ? new SetupResult.Done()
            : new SetupResult.ByHand($"{FileName} pins {VersionSlots.Sdk} in \"{SdksKey}\". "
                + "The version of the Umpire SDK is "
                + "on the first line of each project, for example "
                + $"<Project Sdk=\"{VersionSlots.Sdk}/{context.Version}\">. Remove the entry.");
    }

    static string New(SetupContext context) {
        var template = Templates.SetupFile(FileName, context.Version);
        if (context.ParentFile(FileName) is not string parentFile
            || JsonText.Parse(File.ReadAllText(parentFile)) is not JsonObject parent) {
            return template;
        }
        var json = JsonText.Parse(template)!;
        json.Remove(TestKey);
        foreach (var key in KeptParentKeys) {
            if (parent[key] is JsonNode kept) {
                json[key] = kept.DeepClone();
            }
        }
        (json[SdksKey] as JsonObject)?.Remove(VersionSlots.Sdk);
        return JsonText.Write(json);
    }
}
