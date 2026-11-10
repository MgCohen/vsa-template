using ArchUnitNET.Domain;
using ArchUnitNET.Fluent;
using ArchUnitNET.Fluent.Slices;

namespace Structure;

public static class SliceRules
{
    public static SliceRule UseEachOtherOnlyThrough(AppStructure def, Part each)
    {
        var exposed = each.Exposed ?? [];
        var through = exposed.Count == 0 ? "nothing" : string.Join(", ", exposed.Select(p => p.Path));
        var creator = new SliceRuleCreator();
        creator.SetSliceAssignment(each.Slices);
        creator.AddToDescription($"should use another {each.ItemName} only through {through}");
        creator.SetEvaluationFunction((slices, rule, architecture) => slices.Select(slice =>
        {
            var bad = slice.Types
                .SelectMany(type => type.Dependencies.Select(dependency => (type, target: dependency.Target)))
                .Where(use => def.IsInApp(use.target) && each.Contains(use.target))
                .Where(use => def.ItemOf(use.target, each) != def.ItemOf(use.type, each))
                .Where(use => !exposed.Any(part => part.SelfAndDescendants().Contains(def.LayerOf(use.target))))
                .Select(use => $"{use.type.FullName} uses {use.target.FullName} ({def.LayerOf(use.target)?.Path ?? "no layer"})")
                .Distinct().ToList();
            var name = slice.Identifier.Identifier;
            var description = bad.Count == 0
                ? $"{name} uses another {each.ItemName} only through {through}"
                : $"{name}: {string.Join("; ", bad)}. Fix: use only {through} of another {each.ItemName}.";
            return new EvaluationResult(slice, slice.Identifier, bad.Count == 0, description, rule, architecture);
        }));
        return new SliceRule(creator);
    }

    public static SliceRule HaveExactly(AppStructure def, Part counted)
    {
        var each = counted.EachLevel!;
        var expected = counted.ExactCount!.Value;
        var creator = new SliceRuleCreator();
        creator.SetSliceAssignment(each.Slices);
        creator.AddToDescription($"should have exactly {expected} of {counted.Path}");
        creator.SetEvaluationFunction((slices, rule, architecture) => slices.Select(slice =>
        {
            var found = slice.Types.Where(counted.Contains).Select(type => type.FullName).ToList();
            var passed = found.Count == expected;
            var name = slice.Identifier.Identifier;
            var description = passed
                ? $"{name} has {expected}"
                : $"{name} has {found.Count} of {counted.Path}, expected {expected}: [{string.Join(", ", found)}]. Fix: keep exactly {expected}.";
            return new EvaluationResult(slice, slice.Identifier, passed, description, rule, architecture);
        }));
        return new SliceRule(creator);
    }
}
