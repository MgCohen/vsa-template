using ArchUnitNET.Domain;
using ArchUnitNET.Loader;

namespace Structure;

public abstract class AppStructure() : Part([], null)
{
    SourceMap? map;

    internal SourceMap Map => map ?? throw new InvalidOperationException($"Call {nameof(Load)} before you use the parts of {GetType().Name}.");

    public IEnumerable<Part> Parts => SelfAndDescendants().Skip(1);
    public IEnumerable<Part> Layers => Parts.Where(part => part.IsLayer);
    public IEnumerable<Part> EachLevels => Parts.Where(part => part.IsEach);
    public IEnumerable<Part> Counted => Parts.Where(part => part.ExactCount is not null);

    public Part Find(string path) => Parts.Single(part => part.Path == path);

    public Architecture Load(string dllPath)
    {
        map = SourceMap.Read(dllPath);
        var loaded = new ArchLoader().LoadAssemblies(System.Reflection.Assembly.LoadFrom(dllPath)).Build();
        return new Architecture(loaded.Assemblies, loaded.Namespaces, loaded.Types.Where(IsInApp).ToList(), loaded.GenericParameters, loaded.ReferencedTypes);
    }

    public bool IsInApp(IType type) => Map.FolderOf(type.FullName) is not null && !type.Name.StartsWith('<');

    public Part? LayerOf(IType type) =>
        Layers.Where(layer => layer.Contains(type)).OrderByDescending(layer => layer.Depth).FirstOrDefault();

    public string? ItemOf(IType type, Part each)
    {
        var folder = Map.FolderOf(type.FullName)?.Split('/');
        var index = each.FullSegments.Length - 1;
        return folder is not null && folder.Length > index ? folder[index] : null;
    }

    public string? FolderOf(IType type) => Map.FolderOf(type.FullName);
}
