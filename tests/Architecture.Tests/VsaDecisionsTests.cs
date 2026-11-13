using System.Runtime.CompilerServices;
using VSA.Architecture;
using VSA.Architecture.Vsa;

namespace Architecture.Tests;

public sealed class VsaDecisionsTests {
    static readonly VsaDefinition Definition = new();

    [Test]
    public void The_decision_of_each_pair_of_parts_is_the_same_as_the_baseline() {
        var baseline = BaselineFile();
        var actual = string.Join("\n", Decisions(Definition)) + "\n";
        var expected = File.Exists(baseline) ? File.ReadAllText(baseline).Replace("\r\n", "\n") : "";
        if (actual != expected) {
            File.WriteAllText(Path.ChangeExtension(baseline, ".actual.txt"), actual);
        }

        Assert.That(actual, Is.EqualTo(expected),
            $"The decisions changed. Compare {Path.GetFileName(baseline)} with the .actual.txt file next to it. "
            + "If the change is correct, copy the .actual.txt file over the baseline.");
    }

    static IEnumerable<string> Decisions(ArchitectureDefinition definition) {
        var parts = definition.Parts().ToList();
        foreach (var from in parts.Where(p => !p.IsExternal)) {
            var fromValues = from.Groups.ToDictionary(i => i.Name, _ => "a");
            foreach (var to in parts) {
                var decisions = Scenarios(to)
                    .Where(values => !(from == to && values.All(i => fromValues[i.Key] == i.Value)))
                    .Select(values => new { Scenario = Describe(fromValues, values), Decision = Decide(definition, from, fromValues, to, values) })
                    .ToList();
                if (decisions.Count == 0) {
                    continue;
                }
                if (decisions.Select(d => d.Decision).Distinct().Count() == 1) {
                    yield return $"{Label(from)} -> {Label(to)} [any] {decisions[0].Decision}";
                    continue;
                }
                foreach (var decision in decisions) {
                    yield return $"{Label(from)} -> {Label(to)} [{decision.Scenario}] {decision.Decision}";
                }
            }
        }
    }

    static IEnumerable<Dictionary<string, string>> Scenarios(Part to) {
        IEnumerable<Dictionary<string, string>> all = [[]];
        foreach (var group in to.Groups.Reverse()) {
            all = all.SelectMany(values => new[] { "a", "b" }.Select(value => new Dictionary<string, string>(values) { [group.Name] = value }));
        }
        return all;
    }

    static string Describe(Dictionary<string, string> from, Dictionary<string, string> to) =>
        string.Join(" ", to.Select(i => from.TryGetValue(i.Key, out var value) ? $"{(value == i.Value ? "same" : "other")} {i.Key}" : $"{i.Key} {i.Value}"));

    static string Decide(ArchitectureDefinition definition, Part from, Dictionary<string, string> fromValues, Part to, Dictionary<string, string> toValues) {
        var (permission, pair) = VSA.Architecture.Engine.Decide.For(definition, from, fromValues, to, toValues);
        return permission == Permission.Allow ? $"Allow {pair?.Access.ToString() ?? "(default)"}" : "Deny";
    }

    static string Label(Part part) => string.Join("/", part.SelfAndAncestors().Reverse().Skip(1).Select(p => p.Name));

    static string BaselineFile([CallerFilePath] string path = "") => Path.Combine(Path.GetDirectoryName(path)!, "VsaDecisions.txt");
}
