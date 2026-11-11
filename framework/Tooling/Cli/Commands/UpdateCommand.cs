using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using VSA.Cli.Scaffolding;

namespace VSA.Cli.Commands;

// vsa update [--version <version>]: moves the app to one VSA version. First 'dotnet tool update' changes the tool manifest
// to the latest version on the feeds of the app (or to <version>). Then the tool writes that version to global.json and to
// each VSA.Sdk pin and VSA.* package in the project files: the places that vsa doctor reads.
static class UpdateCommand {
    public static int Run(string? version) {
        var root = AppRoot.Find();
        var manifests = DoctorCommand.ToolManifests(root);
        if (manifests is not [var manifest]) {
            throw new VsaException(manifests is []
                ? "The app has no tool manifest (dotnet-tools.json) with vsa.cli. Add it with 'dotnet tool install vsa.cli --local'."
                : "The app has two tool manifests: dotnet-tools.json and .config/dotnet-tools.json. Keep one.");
        }
        var manifestPath = Path.Combine(root, manifest);
        var current = DoctorCommand.ToolManifestVersion(manifestPath)
            ?? throw new VsaException($"{manifest} has no vsa.cli. Add it with 'dotnet tool install vsa.cli --local'.");

        if (version != current) {
            string[] args = version is null
                ? ["tool", "update", "vsa.cli", "--local", .. current.Contains('-') ? ["--prerelease"] : Array.Empty<string>()]
                : ["tool", "update", "vsa.cli", "--local", "--version", version, "--allow-downgrade"];
            var (code, output) = Dotnet.Run(root, args);
            if (code != 0) {
                throw new VsaException($"'dotnet {string.Join(' ', args)}' failed. The app did not change.{Environment.NewLine}{output}");
            }
        }
        var target = DoctorCommand.ToolManifestVersion(manifestPath)!;

        var changed = SetVersion(root, target);
        Console.WriteLine(current == target ? $"The tool has VSA {target}." : $"The tool changed from VSA {current} to {target}.");
        foreach (var file in changed) {
            Console.WriteLine($"  {file}");
        }
        Console.WriteLine(changed is [] ? "The files already use this version." : $"Changed {changed.Count} files. Next: dotnet build");
        return 0;
    }

    // Writes the version to global.json, each VSA.Sdk pin and each VSA.* package that has a version. Returns the changed files.
    internal static List<string> SetVersion(string root, string version) {
        var changed = new List<string>();
        if (SetGlobalJson(Path.Combine(root, "global.json"), version)) {
            changed.Add("global.json");
        }
        foreach (var file in DoctorCommand.ProjectFiles(root)) {
            if (SetProjectFile(Path.Combine(root, file), version)) {
                changed.Add(file);
            }
        }
        return changed;
    }

    static bool SetGlobalJson(string file, string version) {
        var json = JsonNode.Parse(File.ReadAllText(file))!;
        if (json["msbuild-sdks"] is not JsonObject sdks || sdks["VSA.Sdk"]?.GetValue<string>() is not string old || old == version) {
            return false;
        }
        sdks["VSA.Sdk"] = version;
        File.WriteAllText(file, json.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + Environment.NewLine);
        return true;
    }

    static bool SetProjectFile(string file, string version) {
        var document = XDocument.Load(file, LoadOptions.PreserveWhitespace);
        var changed = false;
        foreach (var element in document.Descendants()) {
            changed |= SetSdkPins(element, version) | SetPackageVersion(element, version);
        }
        if (!changed) {
            return false;
        }
        var bom = File.ReadAllBytes(file) is [0xEF, 0xBB, 0xBF, ..];
        var settings = new XmlWriterSettings { OmitXmlDeclaration = document.Declaration is null, Encoding = new UTF8Encoding(bom) };
        using (var writer = XmlWriter.Create(file, settings)) {
            document.Save(writer);
        }
        return true;
    }

    // <Sdk Name="VSA.Sdk" Version="..." />, and "VSA.Sdk/..." in an Sdk attribute, which can name more than one SDK with ";".
    static bool SetSdkPins(XElement element, string version) {
        if (element.Name.LocalName == "Sdk") {
            return string.Equals((string?)element.Attribute("Name"), "VSA.Sdk", StringComparison.OrdinalIgnoreCase)
                && Set(element.Attribute("Version"), version);
        }
        if (element.Attribute("Sdk") is not XAttribute sdk) {
            return false;
        }
        var parts = sdk.Value.Split(';').Select(part => part.Trim().Split('/') is [var name, _] && string.Equals(name, "VSA.Sdk", StringComparison.OrdinalIgnoreCase)
            ? $"{name}/{version}"
            : part);
        return Set(sdk, string.Join(';', parts));
    }

    // A PackageReference or PackageVersion of a VSA.* package. VersionOverride wins over Version, as in the build.
    static bool SetPackageVersion(XElement element, string version) =>
        element.Name.LocalName is "PackageReference" or "PackageVersion"
        && DoctorCommand.Id(element) is string id && id.StartsWith("VSA.", StringComparison.OrdinalIgnoreCase)
        && Set(element.Attribute("VersionOverride") ?? element.Attribute("Version"), version);

    static bool Set(XAttribute? attribute, string value) {
        if (attribute is null || attribute.Value == value) {
            return false;
        }
        attribute.Value = value;
        return true;
    }
}
