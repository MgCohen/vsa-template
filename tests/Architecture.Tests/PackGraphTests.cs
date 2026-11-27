using System.Xml.Linq;
using Tests.Shared;
using Umpire.Architecture;
using Umpire.Vsa.Definition;

namespace EngineCore.Tests;

public sealed class PackGraphTests {
    static readonly string[] Transports = ["Umpire.Hosts", "Umpire.Vsa.Views"];

    [Test]
    public void The_pack_references_each_kernel_layer_of_its_definition() {
        var named = new ResolvedArchitecture(new VsaArchitecture()).Parts
            .Select(part => part.Assembly)
            .OfType<string>()
            .Where(assembly => assembly.StartsWith("Umpire.", StringComparison.Ordinal))
            .Except(Transports)
            .Distinct();

        var pack = Repo.Path("packs", "Vsa", "Definition", "Umpire.Vsa.csproj");

        Assert.That(ReferenceClosure(pack), Is.SupersetOf(named));
    }

    static HashSet<string> ReferenceClosure(string project) {
        var names = new HashSet<string>(StringComparer.Ordinal);
        var open = new Stack<string>([project]);
        while (open.TryPop(out var current)) {
            if (!names.Add(Path.GetFileNameWithoutExtension(current))) {
                continue;
            }
            var folder = Path.GetDirectoryName(current)!;
            foreach (var reference in XDocument.Load(current).Descendants("ProjectReference")) {
                open.Push(Path.GetFullPath(Path.Combine(folder, reference.Attribute("Include")!.Value)));
            }
        }
        return names;
    }
}
