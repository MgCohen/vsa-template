namespace Umpire.Cli.Scaffolding.Setup.Steps;

using System.Text.Json.Nodes;
using Umpire.Engine.Versions;

sealed class ToolManifestStep : SetupStep {
    public const string FileName = "dotnet-tools.json";
    public const string InConfigFolder = ".config/dotnet-tools.json";
    const string ToolsKey = "tools";

    public override string Id => "tool-manifest";

    public override IReadOnlyList<string> Files(SetupContext context) => [FileName, InConfigFolder];

    public override SetupResult Plan(SetupContext context) {
        var version = context.Version;
        var template = JsonText.Parse(Templates.SetupFile(FileName, version))!;
        string[] manifests = [.. Files(context).Where(path => context.Read(path) is not null)];
        if (manifests is []) {
            if ((context.ParentFile(FileName) ?? context.ParentFile(InConfigFolder)) is not null) {
                template["isRoot"] = false;
            }
            return Change(FileName, JsonText.Write(template));
        }
        if (manifests is not [var manifest]) {
            return ByHand($"The app has two tool manifests: {FileName} and {InConfigFolder}. Keep one.");
        }
        var install = $"Add the tool with 'dotnet tool install {VersionSlots.Tool} --local --version {version}'.";
        var text = context.Read(manifest)!;
        if (JsonText.Parse(text) is not JsonObject json) {
            return ByHand($"{manifest} has comments or is not valid JSON. {install}");
        }
        var tools = json[ToolsKey];
        var current = JsonText.Text(tools?[VersionSlots.Tool]?["version"]);
        if (current == version) {
            return Done();
        }
        if (current is not null) {
            return ByHand(
                $"{manifest} has {VersionSlots.Tool} {current}, and the tool is {version}. Run 'umpire update'.");
        }
        var entry = template[ToolsKey]![VersionSlots.Tool]!.DeepClone();
        if (tools is null) {
            json[ToolsKey] = new JsonObject { [VersionSlots.Tool] = entry };
        } else if (tools is JsonObject entries) {
            entries[VersionSlots.Tool] = entry;
        } else {
            return ByHand($"\"{ToolsKey}\" in {manifest} is not an object. {install}");
        }
        return Change(manifest, JsonText.Write(json, text));
    }
}
