namespace Umpire.Cli.Features.Scaffold.Domain.Setup.Tasks;

using System.Text.Json.Nodes;
using Umpire.Engine.Versions;

sealed class GlobalJsonTask : SetupTask {
    const string FileName = "global.json";
    const string SdksKey = "msbuild-sdks";

    public override string Id => "global-json";

    public override SetupResult Plan(SetupContext context) {
        if (context.Read(FileName) is not string text) {
            return new SetupResult.Done();
        }
        if (JsonText.Parse(text) is not JsonObject json) {
            return new SetupResult.ByHand($"{FileName} has comments or is not valid JSON. "
                + $"Make sure that it has no \"{SdksKey}\" entry for {VersionSlots.RemovedSdk}.");
        }
        return json[SdksKey] is not JsonObject sdks || !sdks.Any(pair => VersionSlots.IsRemovedSdk(pair.Key))
            ? new SetupResult.Done()
            : new SetupResult.ByHand($"{FileName} pins {VersionSlots.RemovedSdk} in \"{SdksKey}\". "
                + "The Umpire SDK does not exist any more: the Umpire packages bring their own build files. "
                + $"Remove the entry, and give the version of each Umpire package in its PackageReference, for example "
                + $"<PackageReference Include=\"Umpire.Web\" Version=\"{context.Version}\" />.");
    }
}
