using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Conditions;
using ArchUnitNET.Fluent.Slices;
using ArchUnitNET.NUnit;
using NUnit.Framework;
using static ArchUnitNET.Fluent.ArchRuleDefinition;

namespace Structure;

public abstract class StructureTests<TStructure> where TStructure : AppStructure, new()
{
    protected StructureTests(string appDll)
    {
        Def = new TStructure();
        Arch = Def.Load(appDll);
    }

    protected TStructure Def { get; }
    protected Architecture Arch { get; }

    static TStructure Definition => new();
    static IEnumerable<string> Layers() => Definition.Layers.Select(part => part.Path);
    static IEnumerable<string> EachLevels() => Definition.EachLevels.Select(part => part.Path);
    static IEnumerable<string> ExposingLevels() => Definition.EachLevels.Where(part => part.Exposed is not null).Select(part => part.Path);
    static IEnumerable<string> Counted() => Definition.Counted.Select(part => part.Path);

    [Test]
    public void Each_type_is_in_a_layer() =>
        Types().That().Are(Def).Should()
            .FollowCustomCondition(type => new ConditionResult(type, Def.LayerOf(type) is not null,
                $"is in folder '{Def.FolderOf(type)}', which is in no layer. Fix: move it to a layer folder."),
                "be in a layer")
            .Check(Arch);

    [TestCaseSource(nameof(Layers))]
    public void Layer_uses_only_allowed_parts(string layer)
    {
        var part = Def.Find(layer);
        Types().That().Are(part.Own).Should()
            .FollowCustomCondition(type => Allowed(type, part), $"use only {string.Join(", ", part.Allowed.Append(part).Select(p => p.Path))}")
            .WithoutRequiringPositiveResults()
            .Check(Arch);
    }

    [TestCaseSource(nameof(EachLevels))]
    public void Items_are_free_of_cycles(string each) =>
        Slices(Def.Find(each).Slices).Should().BeFreeOfCycles().Check(Arch);

    [TestCaseSource(nameof(ExposingLevels))]
    public void Items_use_each_other_only_through_exposed_parts(string each) =>
        SliceRules.UseEachOtherOnlyThrough(Def, Def.Find(each)).Check(Arch);

    [TestCaseSource(nameof(Counted))]
    public void Count_in_each_item(string counted) =>
        SliceRules.HaveExactly(Def, Def.Find(counted)).Check(Arch);

    [Test]
    public void Namespace_matches_folder() =>
        Types().That().Are(Def).Should()
            .FollowCustomCondition(type =>
            {
                var folder = Def.FolderOf(type)!;
                var expected = folder.Replace('/', '.');
                var matches = expected.Length == 0 || type.Namespace.FullName.EndsWith("." + expected);
                return new ConditionResult(type, matches,
                    $"has namespace '{type.Namespace.FullName}', but its folder is '{folder}'. Fix: make the namespace end with '{expected}'.");
            }, "have a namespace that matches the folder")
            .Check(Arch);

    protected static GivenSlices Slices(SliceAssignment assignment)
    {
        var creator = new SliceRuleCreator();
        creator.SetSliceAssignment(assignment);
        return new GivenSlices(creator);
    }

    ConditionResult Allowed(IType type, Part part)
    {
        var bad = type.Dependencies.Select(dependency => dependency.Target).Distinct()
            .Select(target => (target, layer: Def.IsInApp(target) ? Def.LayerOf(target) : null))
            .Where(use => use.layer is not null && use.layer != part && !part.Allowed.Contains(use.layer))
            .Select(use => $"{use.target.FullName} ({use.layer!.Path})").ToList();
        var allowed = part.Allowed.Count == 0 ? "its own layer" : string.Join(", ", part.Allowed.Select(p => p.Path));
        return new ConditionResult(type, bad.Count == 0, $"uses {string.Join(", ", bad)}. {part.Path} may use only: {allowed}.");
    }
}
