// Writes VSA.slnx with each project in this repository. Git ignores VSA.slnx: run this script again after you add,
// move or remove a project.
//
//   dotnet run solution.cs
//
// The solution has one folder for each top-level folder (framework, samples, tests).

using System.Runtime.CompilerServices;
using System.Text;

var root = Path.GetDirectoryName(Here())!;

// The tests build these projects themselves, in a copy. They are not part of the solution.
string[] excluded = ["tests/Fixtures/", "docs/legacy/"];

var projects = Directory.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
    .Select(path => Path.GetRelativePath(root, path).Replace('\\', '/'))
    .Where(path => !path.Split('/').Any(part => part.StartsWith('.') || part is "bin" or "obj"))
    .Where(path => !excluded.Any(path.StartsWith))
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

File.WriteAllText(Path.Combine(root, "VSA.slnx"), solution.ToString());
Console.WriteLine($"VSA.slnx: {projects.Count} projects.");

static string Here([CallerFilePath] string path = "") => path;
