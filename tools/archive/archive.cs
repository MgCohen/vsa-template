using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;

const string RuntimeFolder = "runtime/netstandard2.0";
const string DefinitionFolder = "definition/netstandard2.0";
const string AnalyzerFolder = "analyzers/dotnet/cs";
const string EngineFolder = "engine/net8.0";
const string EngineBuild = "netstandard2.0";
const string EnginePackageFolder = "tools/net8.0/";
const string Notices = "THIRD-PARTY-NOTICES.txt";
const string Usage = "Usage: dotnet run tools/archive/archive.cs -- --feed <folder> --version <version> "
    + "--out <folder> [--commit <sha>]";

var options = Archive.Options.Parse(args);
if (options is null) {
    Console.Error.WriteLine(Usage);
    return 2;
}

var repo = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Here())!, "..", ".."));
var archive = Path.Combine(options.Out, $"umpire-{options.Version}.zip");
Directory.CreateDirectory(options.Out);
File.Delete(archive);
var work = Directory.CreateTempSubdirectory("umpire-archive-").FullName;
try {
    var tree = Path.Combine(work, "tree");
    var analyzers = Archive.FromPackage(options, "Umpire.Vsa.Analyzers", "analyzers/dotnet/cs/",
        Path.Combine(tree, AnalyzerFolder));
    var engine = Archive.FromPackage(options, "Umpire.Sdk", EnginePackageFolder, Path.Combine(tree, EngineFolder));
    var publish = Path.Combine(work, "publish");
    var runtime = Archive.Runtime(options, repo, publish, engine.Values, Path.Combine(tree, RuntimeFolder),
        Path.Combine(tree, DefinitionFolder));
    List<Archive.FileEntry> files = [
        .. Archive.Entries(tree, RuntimeFolder, runtime),
        .. Archive.Entries(tree, DefinitionFolder, runtime),
        .. Archive.Entries(tree, AnalyzerFolder, analyzers),
        .. Archive.Entries(tree, EngineFolder, engine),
    ];
    var embedded = Archive.PackageText(options, "Umpire.Vsa.Analyzers", Notices);
    File.WriteAllText(Path.Combine(tree, Notices), Archive.Notices(files, Path.Combine(publish, "packages"), embedded));
    var manifest = new JsonObject {
        ["version"] = options.Version,
        ["commit"] = options.Commit ?? Archive.Run(repo, "git", "rev-parse", "HEAD").Trim(),
        ["analyzers"] = new JsonObject { ["roslyn"] = Archive.Roslyn(Path.Combine(tree, AnalyzerFolder)) },
        ["engine"] = Archive.EngineRuntime(
            Path.Combine(tree, EngineFolder, "umpire-engine.runtimeconfig.json"), EngineBuild),
        ["files"] = new JsonArray([
            .. files.OrderBy(file => file.Path, StringComparer.Ordinal).Select(file => file.Json())]),
    };
    var text = manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(tree, "manifest.json"), text + "\n");

    ZipFile.CreateFromDirectory(tree, archive, CompressionLevel.Optimal, includeBaseDirectory: false);
    Console.WriteLine($"{archive}: {files.Count} files.");
    return 0;
} finally {
    Directory.Delete(work, recursive: true);
}

static string Here([CallerFilePath] string path = "") {
    return path;
}

static class Archive {
    const string ArchiveProject = "UmpireArchive";
    static readonly string[] RuntimePackages = ["Umpire", "Umpire.Vsa"];
    static readonly string[] DefinitionPackages = ["Umpire.Definition", "Umpire.Vsa.Definition"];

    public static IReadOnlyDictionary<string, PackageName> Runtime(
        Options options, string repo, string publish, IEnumerable<PackageName> engine, string target, string definitionTarget) {
        var project = Path.Combine(publish, "project");
        Directory.CreateDirectory(project);
        File.Copy(Path.Combine(repo, "global.json"), Path.Combine(project, "global.json"));
        var downloads = engine.Where(package => !package.Project && package.Id != "Umpire.Sdk")
            .Select(package => $"""    <PackageDownload Include="{package.Id}" Version="[{package.Version}]" />""")
            .Distinct();
        var packages = RuntimePackages.Concat(DefinitionPackages)
            .Select(package => $"""    <PackageReference Include="{package}" Version="[{options.Version}]" />""");
        File.WriteAllText(Path.Combine(project, $"{ArchiveProject}.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.0</TargetFramework>
                <RestoreSources>{Path.GetFullPath(options.Feed)};https://api.nuget.org/v3/index.json</RestoreSources>
                <RestorePackagesPath>{Path.Combine(publish, "packages")}</RestorePackagesPath>
              </PropertyGroup>
              <ItemGroup>
            {string.Join("\n", packages)}
            {string.Join("\n", downloads)}
              </ItemGroup>
            </Project>
            """);
        var output = Path.Combine(publish, "out");
        Run(project, "dotnet", "publish", "-c", "Release", "-o", output, "-nodeReuse:false");
        Directory.CreateDirectory(target);
        Directory.CreateDirectory(definitionTarget);
        var names = DepsPackages(Path.Combine(output, $"{ArchiveProject}.deps.json"));
        var dlls = Directory.GetFiles(output, "*.dll").Where(dll => Path.GetFileName(dll) != $"{ArchiveProject}.dll");
        foreach (var dll in dlls) {
            var name = Path.GetFileName(dll);
            var isDefinition = names.TryGetValue(name, out var package) && DefinitionPackages.Contains(package.Id);
            File.Copy(dll, Path.Combine(isDefinition ? definitionTarget : target, name));
        }
        return names;
    }

    public static IReadOnlyDictionary<string, PackageName> FromPackage(
        Options options, string package, string folder, string target) {
        using var zip = ZipFile.OpenRead(Package(options, package));
        var entries = zip.Entries
            .Where(entry => entry.FullName.StartsWith(folder, StringComparison.Ordinal) && entry.Name.Length > 0)
            .ToList();
        if (entries.Count == 0) {
            throw new InvalidOperationException($"{package} {options.Version} has no files in {folder}.");
        }
        foreach (var entry in entries) {
            var path = Path.Combine(target, entry.FullName[folder.Length..]);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            entry.ExtractToFile(path);
        }
        var deps = Directory.GetFiles(target, "*.deps.json").FirstOrDefault();
        var names = deps is null
            ? new Dictionary<string, PackageName>(StringComparer.OrdinalIgnoreCase)
            : DepsPackages(deps);
        foreach (var entry in entries.Where(entry => !names.TryGetValue(entry.Name, out var name) || name.Project)) {
            names[entry.Name] = new PackageName(package, options.Version, entry.FullName);
        }
        return names;
    }

    public static string PackageText(Options options, string package, string file) {
        using var zip = ZipFile.OpenRead(Package(options, package));
        var entry = zip.GetEntry(file) ?? throw new InvalidOperationException($"{package} has no {file}.");
        using var reader = new StreamReader(entry.Open());
        return reader.ReadToEnd();
    }

    public static string Notices(IReadOnlyList<FileEntry> files, string packages, string embedded) {
        var text = new StringBuilder();
        text.AppendLine("The archive holds the third-party packages below. Each one keeps its own license.");
        text.AppendLine();
        var thirdParty = files.Select(file => file.Package)
            .OfType<PackageName>()
            .Where(package => !package.Id.StartsWith("Umpire", StringComparison.Ordinal))
            .DistinctBy(package => $"{package.Id}/{package.Version}")
            .OrderBy(package => package.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var package in thirdParty) {
            text.AppendLine($"{package.Id} {package.Version}: {License(packages, package)}");
        }
        text.AppendLine();
        text.AppendLine("-------------------------------------------------------------------------------");
        text.Append(embedded);
        return text.ToString();
    }

    public static IEnumerable<FileEntry> Entries(
        string tree, string folder, IReadOnlyDictionary<string, PackageName> names) {
        return Directory.GetFiles(Path.Combine(tree, folder), "*", SearchOption.AllDirectories).Select(file => {
            var relative = Path.GetRelativePath(tree, file).Replace(Path.DirectorySeparatorChar, '/');
            names.TryGetValue(Path.GetFileName(file), out var package);
            return new FileEntry(relative, AssemblyVersion(file), package, Sha256(file));
        });
    }

    public static string Roslyn(string analyzers) {
        var versions = Directory.GetFiles(analyzers, "*.dll").SelectMany(CompilerReferences).ToList();
        if (versions.Count == 0) {
            throw new InvalidOperationException($"No analyzer in {analyzers} references Microsoft.CodeAnalysis.");
        }
        var highest = versions.Max()!;
        return $"{highest.Major}.{highest.Minor}";
    }

    public static JsonObject EngineRuntime(string runtimeConfig, string umpireBuild) {
        var options = JsonNode.Parse(File.ReadAllText(runtimeConfig))!["runtimeOptions"]!;
        var frameworks = options["frameworks"]?.AsArray() ?? new JsonArray(options["framework"]!.DeepClone());
        return new JsonObject {
            ["targetFramework"] = options["tfm"]!.GetValue<string>(),
            ["rollForward"] = options["rollForward"]?.GetValue<string>(),
            ["frameworks"] = new JsonArray([.. frameworks.Select(Framework)]),
            ["umpireBuild"] = umpireBuild,
        };
    }

    public static string Run(string folder, string program, params string[] arguments) {
        var start = new ProcessStartInfo(program) {
            WorkingDirectory = folder,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var argument in arguments) {
            start.ArgumentList.Add(argument);
        }
        using var process = Process.Start(start)!;
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0) {
            throw new InvalidOperationException(
                $"{program} {string.Join(' ', arguments)} failed with exit code {process.ExitCode}:\n"
                + output + error.Result);
        }
        return output;
    }

    static string Package(Options options, string package) {
        var file = Path.Combine(options.Feed, $"{package}.{options.Version}.nupkg");
        return File.Exists(file)
            ? file
            : throw new FileNotFoundException($"The feed has no {package} {options.Version}.", file);
    }

    static string License(string packages, PackageName package) {
        var id = package.Id.ToLowerInvariant();
        var nuspec = Path.Combine(packages, id, package.Version.ToLowerInvariant(), $"{id}.nuspec");
        if (!File.Exists(nuspec)) {
            return $"see https://www.nuget.org/packages/{package.Id}/{package.Version}";
        }
        var metadata = XDocument.Load(nuspec).Root!.Elements().First(element => element.Name.LocalName == "metadata");
        var license = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "license")?.Value;
        var url = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "licenseUrl")?.Value;
        var project = metadata.Elements().FirstOrDefault(element => element.Name.LocalName == "projectUrl")?.Value;
        return $"{license ?? url ?? "see the package"}{(project is null ? "" : $" ({project})")}";
    }

    static JsonNode Framework(JsonNode? framework) {
        return new JsonObject {
            ["name"] = framework!["name"]!.GetValue<string>(),
            ["version"] = framework["version"]!.GetValue<string>(),
        };
    }

    static List<Version> CompilerReferences(string dll) {
        using var stream = File.OpenRead(dll);
        using var pe = new PEReader(stream);
        var reader = pe.GetMetadataReader();
        return [.. reader.AssemblyReferences.Select(reader.GetAssemblyReference)
            .Where(reference => reader.GetString(reference.Name)
                .StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal))
            .Select(reference => reference.Version)];
    }

    static Dictionary<string, PackageName> DepsPackages(string deps) {
        var names = new Dictionary<string, PackageName>(StringComparer.OrdinalIgnoreCase);
        var json = JsonNode.Parse(File.ReadAllText(deps))!;
        var libraries = json["libraries"]!.AsObject();
        foreach (var target in json["targets"]!.AsObject().Select(target => target.Value!.AsObject())) {
            foreach (var (key, library) in target) {
                if (library?["runtime"] is not JsonObject runtime) {
                    continue;
                }
                var split = key.IndexOf('/');
                var project = libraries[key]?["type"]?.GetValue<string>() == "project";
                foreach (var asset in runtime.Select(entry => entry.Key)) {
                    names.TryAdd(Path.GetFileName(asset),
                        new PackageName(key[..split], key[(split + 1)..], asset, project));
                }
            }
        }
        return names;
    }

    static string? AssemblyVersion(string file) {
        if (!file.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) {
            return null;
        }
        try {
            return AssemblyName.GetAssemblyName(file).Version?.ToString();
        } catch (BadImageFormatException) {
            return null;
        }
    }

    static string Sha256(string file) {
        using var stream = File.OpenRead(file);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    public sealed record Options(string Feed, string Version, string Out, string? Commit) {
        public static Options? Parse(string[] args) {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i + 1 < args.Length; i += 2) {
                values[args[i]] = args[i + 1];
            }
            return values.TryGetValue("--feed", out var feed) && values.TryGetValue("--version", out var version)
                && values.TryGetValue("--out", out var output)
                ? new Options(feed, version, output, values.GetValueOrDefault("--commit"))
                : null;
        }
    }

    public sealed record PackageName(string Id, string Version, string Asset, bool Project = false);

    public sealed record FileEntry(string Path, string? AssemblyVersion, PackageName? Package, string Sha256) {
        public JsonObject Json() {
            return new JsonObject {
                ["path"] = Path,
                ["assemblyVersion"] = AssemblyVersion,
                ["package"] = Package?.Id,
                ["packageVersion"] = Package?.Version,
                ["asset"] = Package?.Asset,
                ["sha256"] = Sha256,
            };
        }
    }
}
