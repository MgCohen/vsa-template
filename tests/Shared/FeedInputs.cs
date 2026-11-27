namespace Tests.Shared;

using System.Security.Cryptography;
using System.Text;

static class FeedInputs {
    static readonly string[] buildOutputFolders = ["artifacts~", "bin", "obj", "node_modules"];

    public static string Hash(IReadOnlyList<string> packRoots, string packArguments) {
        var files = Files(packRoots);
        if (files is null) {
            return $"x{Guid.NewGuid():N}"[..10];
        }
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Add(hash, packArguments);
        Add(hash, Dotnet.Run(Repo.Root, null, "--version").Output.Trim());
        foreach (var file in files) {
            Add(hash, file);
            var path = Repo.Path(file);
            hash.AppendData(File.Exists(path) ? SHA256.HashData(File.ReadAllBytes(path)) : [0]);
        }
        return Convert.ToHexString(hash.GetHashAndReset())[..10].ToLowerInvariant();
    }

    static List<string>? Files(IReadOnlyList<string> packRoots) {
        DotnetResult listed;
        DotnetResult ignored;
        try {
            listed = Git("ls-files", "-z", "--cached", "--others", "--exclude-standard");
            ignored = Git("ls-files", "-z", "--others", "--ignored", "--exclude-standard");
        } catch (System.ComponentModel.Win32Exception) {
            return null;
        }
        if (!listed.Succeeded || !ignored.Succeeded) {
            return null;
        }
        return [.. Lines(listed).Concat(Lines(ignored).Where(file => !IsBuildOutput(file)))
            .Where(file => IsInput(file, packRoots))
            .Distinct()
            .Order(StringComparer.Ordinal)];
    }

    static IEnumerable<string> Lines(DotnetResult listed) =>
        listed.Output.Split('\0', StringSplitOptions.RemoveEmptyEntries);

    static bool IsBuildOutput(string file) => file.Split('/')[..^1].Any(buildOutputFolders.Contains);

    static bool IsInput(string file, IReadOnlyList<string> packRoots) =>
        !file.Contains('/') || packRoots.Any(root => file.StartsWith($"{root}/", StringComparison.Ordinal));

    static DotnetResult Git(params string[] args) {
        var info = new System.Diagnostics.ProcessStartInfo("git") {
            WorkingDirectory = Repo.Root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        foreach (var arg in args) {
            info.ArgumentList.Add(arg);
        }
        using var process = System.Diagnostics.Process.Start(info)
            ?? throw new InvalidOperationException("git did not start.");
        var error = process.StandardError.ReadToEndAsync();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        var text = process.ExitCode == 0 ? output : error.Result;
        return new DotnetResult($"git {string.Join(' ', args)}", process.ExitCode, text);
    }

    static void Add(IncrementalHash hash, string text) {
        hash.AppendData(Encoding.UTF8.GetBytes(text));
        hash.AppendData([0]);
    }
}
