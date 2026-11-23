using System.Runtime.CompilerServices;
using System.Text;

var root = Path.GetDirectoryName(Here())!;

string[] builtByTheirTests = ["tests/Fixtures/"];
string[] fixturesWithOwnTests = ["tests/Fixtures/Notes/", "tests/Fixtures/PackVariant/App/Packs/Ecs/Tests/"];

var projects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
    .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
    .Where(path => !path.Split('/').Any(part => part.StartsWith('.') || part is "bin" or "obj"))
    .Where(path => fixturesWithOwnTests.Any(path.StartsWith) || !builtByTheirTests.Any(path.StartsWith))
    .Order(StringComparer.Ordinal)
    .ToList();

var solution = new StringBuilder("<Solution>\n");
foreach (var group in projects.GroupBy(path => path[..path.IndexOf('/')])) {
    solution.Append($"  <Folder Name=\"/{group.Key}/\">\n");
    foreach (var path in group) {
        solution.Append($"    <Project Path=\"{path}\" />\n");
    }
    solution.Append("  </Folder>\n");
}
solution.Append("</Solution>\n");

File.WriteAllText(Path.Combine(root, "Umpire.slnx"), solution.ToString());
Console.WriteLine($"Umpire.slnx: {projects.Count} projects.");

static string Here([CallerFilePath] string path = "") => path;
