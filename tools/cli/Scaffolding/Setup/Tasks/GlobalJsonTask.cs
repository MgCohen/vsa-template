namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using System.Text.Json.Nodes;
using Umpire.Engine.Versions;
using Umpire.Files;

sealed class GlobalJsonTask : SetupTask {
    const string FileName = "global.json";
    const string TestKey = "test";
    static readonly string[] KeptParentKeys = ["sdk", VersionSlots.MsbuildSdks, TestKey];

    public override string Id => "global-json";

    public override SetupResult Plan(SetupContext context) {
        if (context.Read(FileName) is not string text) {
            return new SetupResult.Change(new TextFile(FileName, New(context)));
        }
        if (JsonText.Parse(text) is not JsonObject json) {
            return new SetupResult.ByHand($"{FileName} has comments or is not valid JSON. "
                + $"Make sure that it has no \"{VersionSlots.MsbuildSdks}\" entry for an Umpire SDK.");
        }
        return !VersionSlots.PinsSdk(json)
            ? new SetupResult.Done()
            : new SetupResult.ByHand($"{FileName} pins an Umpire SDK in \"{VersionSlots.MsbuildSdks}\". "
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
        if (json[VersionSlots.MsbuildSdks] is JsonObject sdks) {
            foreach (var pin in sdks.Select(pair => pair.Key).Where(VersionSlots.IsSdk).ToList()) {
                sdks.Remove(pin);
            }
        }
        return JsonText.Write(json);
    }
}
