namespace Umpire.Cli.Scaffolding.Setup.Tasks;

using System.Text.Json.Nodes;
using Umpire.Engine.Versions;

sealed class GlobalJsonTask : SetupTask {
    const string FileName = VersionSlots.GlobalJsonFile;
    const string SdksKey = "msbuild-sdks";
    const string TestKey = "test";
    static readonly string[] KeptParentKeys = ["sdk", SdksKey, TestKey];

    public override string Id => "global-json";

    public override IReadOnlyList<string> Files(SetupContext context) => [FileName];

    public override SetupResult Plan(SetupContext context) {
        var version = context.Version;
        if (context.Read(FileName) is not string text) {
            return Change(FileName, New(context));
        }
        if (JsonText.Parse(text) is not JsonObject json) {
            return ByHand($"{FileName} has comments or is not valid JSON. "
                + $"Add \"{SdksKey}\": {{ \"{VersionSlots.Sdk}\": \"{version}\" }} to it.");
        }
        var pin = JsonText.Text(json[SdksKey]?[VersionSlots.Sdk]);
        if (pin == version) {
            return Done();
        }
        if (pin is not null) {
            return ByHand($"{FileName} pins {VersionSlots.Sdk} {pin}, and the tool is {version}. Run 'umpire update'.");
        }
        return WithPin(json, version)
            ? Change(FileName, JsonText.Write(json, text))
            : ByHand($"\"{SdksKey}\" in {FileName} is not an object. "
                + $"Add \"{VersionSlots.Sdk}\": \"{version}\" to it.");
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
        return WithPin(json, context.Version) ? JsonText.Write(json) : template;
    }

    static bool WithPin(JsonObject json, string version) {
        switch (json[SdksKey]) {
            case null:
                json[SdksKey] = new JsonObject { [VersionSlots.Sdk] = version };
                return true;
            case JsonObject sdks:
                sdks[VersionSlots.Sdk] = version;
                return true;
            default:
                return false;
        }
    }
}
