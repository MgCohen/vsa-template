using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;

const string RuntimeFolder = "runtime/netstandard2.1";
const string AnalyzerFolder = "analyzers/dotnet/cs";
const string EngineFolder = "engine/net8.0";
const string Usage = "Usage: dotnet run tools/archive/archive.cs -- --feed <folder> --version <version> "
    + "--out <folder> [--commit <sha>]";

var options = Archive.Options.Parse(args);
if (options is null) {
    Console.Error.WriteLine(Usage);
    return 2;
}

var work = Directory.CreateTempSubdirectory("umpire-archive-").FullName;
try {
    var tree = Path.Combine(work, "tree");
    var runtime = Archive.Runtime(options, Path.Combine(work, "publish"), Path.Combine(tree, RuntimeFolder));
    var analyzers = Archive.FromPackage(options, "Umpire.Vsa.Analyzers", "analyzers/dotnet/cs/",
        Path.Combine(tree, AnalyzerFolder));
    var engine = Archive.FromPackage(options, "Umpire.Sdk", "tools/net8.0/", Path.Combine(tree, EngineFolder));
    List<Archive.FileEntry> files = [
        .. Archive.Entries(tree, RuntimeFolder, runtime),
        .. Archive.Entries(tree, AnalyzerFolder, analyzers),
        .. Archive.Entries(tree, EngineFolder, engine),
    ];
    var manifest = new JsonObject {
        ["version"] = options.Version,
        ["commit"] = options.Commit ?? Archive.Commit(Path.GetDirectoryName(Here())!),
        ["analyzers"] = new JsonObject { ["roslyn"] = Archive.Roslyn(Path.Combine(tree, AnalyzerFolder)) },
        ["engine"] = Archive.EngineRuntime(Path.Combine(tree, EngineFolder, "umpire-engine.runtimeconfig.json")),
        ["files"] = new JsonArray([
            .. files.OrderBy(file => file.Path, StringComparer.Ordinal).Select(file => file.Json())]),
    };
    var text = manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(Path.Combine(tree, "manifest.json"), text + "\n");

    Directory.CreateDirectory(options.Out);
    var archive = Path.Combine(options.Out, $"umpire-{options.Version}.zip");
    File.Delete(archive);
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

    public static IReadOnlyDictionary<string, PackageName> Runtime(Options options, string publish, string target) {
        var project = Path.Combine(publish, "project");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, $"{ArchiveProject}.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>netstandard2.1</TargetFramework>
                <RestoreSources>{Path.GetFullPath(options.Feed)};https://api.nuget.org/v3/index.json</RestoreSources>
                <RestorePackagesPath>{Path.Combine(publish, "packages")}</RestorePackagesPath>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Umpire" Version="[{options.Version}]" />
              </ItemGroup>
            </Project>
            """);
        var output = Path.Combine(publish, "out");
        Run(project, "dotnet", "publish", "-c", "Release", "-o", output, "-nodeReuse:false");
        Directory.CreateDirectory(target);
        var dlls = Directory.GetFiles(output, "*.dll").Where(dll => Path.GetFileName(dll) != $"{ArchiveProject}.dll");
        foreach (var dll in dlls) {
            File.Copy(dll, Path.Combine(target, Path.GetFileName(dll)));
        }
        return DepsPackages(Path.Combine(output, $"{ArchiveProject}.deps.json"));
    }

    public static IReadOnlyDictionary<string, PackageName> FromPackage(
        Options options, string package, string folder, string target) {
        var file = Path.Combine(options.Feed, $"{package}.{options.Version}.nupkg");
        if (!File.Exists(file)) {
            throw new FileNotFoundException($"The feed has no {package} {options.Version}.", file);
        }
        using var zip = ZipFile.OpenRead(file);
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
        var own = new PackageName(package, options.Version);
        var deps = Directory.GetFiles(target, "*.deps.json").FirstOrDefault();
        var names = deps is null
            ? new Dictionary<string, PackageName>(StringComparer.OrdinalIgnoreCase)
            : DepsPackages(deps);
        foreach (var name in names.Where(name => name.Value.Project).Select(name => name.Key).ToList()) {
            names[name] = own;
        }
        foreach (var entry in entries.Where(entry => !names.ContainsKey(entry.Name))) {
            names[entry.Name] = own;
        }
        return names;
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

    public static JsonObject EngineRuntime(string runtimeConfig) {
        var options = JsonNode.Parse(File.ReadAllText(runtimeConfig))!["runtimeOptions"]!;
        var frameworks = options["frameworks"]?.AsArray() ?? new JsonArray(options["framework"]!.DeepClone());
        return new JsonObject {
            ["targetFramework"] = options["tfm"]!.GetValue<string>(),
            ["rollForward"] = options["rollForward"]?.GetValue<string>(),
            ["frameworks"] = new JsonArray([.. frameworks.Select(Framework)]),
        };
    }

    public static string Commit(string folder) {
        return Run(folder, "git", "rev-parse", "HEAD").Trim();
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
                foreach (var dll in runtime.Select(entry => Path.GetFileName(entry.Key))) {
                    names.TryAdd(dll, new PackageName(key[..split], key[(split + 1)..], project));
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

    static string Run(string folder, string program, params string[] arguments) {
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

    public sealed record PackageName(string Id, string Version, bool Project = false);

    public sealed record FileEntry(string Path, string? AssemblyVersion, PackageName? Package, string Sha256) {
        public JsonObject Json() {
            return new JsonObject {
                ["path"] = Path,
                ["assemblyVersion"] = AssemblyVersion,
                ["package"] = Package?.Id,
                ["packageVersion"] = Package?.Version,
                ["sha256"] = Sha256,
            };
        }
    }
}
