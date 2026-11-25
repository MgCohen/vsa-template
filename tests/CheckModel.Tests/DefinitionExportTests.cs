namespace CheckModel.Tests;

using Umpire.Architecture;
using Umpire.CheckModel.Export;
using Data = Umpire.CheckModel;

public sealed class DefinitionExportTests {
    static readonly Architecture[] definitions = [
        new EveryFormArchitecture(), new Umpire.Vsa.Definition.VsaArchitecture(),
        new Umpire.Kernels.PackArchitecture(new Umpire.Vsa.Definition.VsaArchitecture()),
        new EngineCore.Tests.BadArchitecture(), new PackVariant.Packs.Ecs.Definition.EcsArchitecture(),
        new UmpireEngine.EngineArchitecture(), new Spike.Charlie.Definition.CharlieArchitecture(),
    ];

    [TestCaseSource(nameof(definitions))]
    public void Each_decision_of_the_data_is_the_decision_of_the_definition(Architecture definition) {
        var resolved = new ResolvedArchitecture(definition);
        var data = Data.Definition.Read(DefinitionExport.Of(resolved).Write());
        var parts = Data.Part.All(data);
        var decisions = new Data.Decisions(data, parts);
        var positions = resolved.Parts.Select((part, i) => KeyValuePair.Create(part, i)).ToDictionary();

        var different = new List<string>();
        foreach (var from in resolved.Parts) {
            foreach (var to in resolved.Parts) {
                foreach (var values in Scenarios(from, to)) {
                    var expected = resolved.DecisionFor(from, values.From, to, values.To);
                    var actual = decisions.Of(parts[positions[from]], values.From, parts[positions[to]], values.To);
                    if (Text(expected) != Text(actual)) {
                        different.Add($"{from.Path} -> {to.Path}: {Text(expected)}, not {Text(actual)}");
                    }
                }
            }
        }

        Assert.That(different, Is.Empty);
    }

    [TestCaseSource(nameof(definitions))]
    public void The_parts_of_the_data_are_the_parts_of_the_definition(Architecture definition) {
        var resolved = new ResolvedArchitecture(definition);
        var parts = Data.Part.All(Data.Definition.Read(DefinitionExport.Of(resolved).Write()));

        Assert.That(parts.Select(p => $"{p.Name}|{p.Path}|{p.External}|{string.Join(",", p.Types.Select(t => t.FullName))}"),
            Is.EqualTo(resolved.Parts.Select(p => $"{p.Name}|{p.Path}|{p.Assembly}|{string.Join(",", p.Types.Select(t => t.FullName))}")));
    }

    [TestCaseSource(nameof(definitions))]
    public void Each_statement_of_the_data_has_the_text_of_the_definition(Architecture definition) {
        var resolved = new ResolvedArchitecture(definition);
        var data = Data.Definition.Read(DefinitionExport.Of(resolved).Write());
        var compiled = resolved.Compiled;

        Assert.Multiple(() => {
            Assert.That(data.Counts.Where(c => !c.IsRequired).Select(c => c.ToString()),
                Is.EqualTo(compiled.Book.Counts.Select(c => c.ToString())));
            Assert.That(data.Counts.Where(c => c.IsRequired).Count(), Is.EqualTo(compiled.Counts.Count - compiled.Book.Counts.Count));
            Assert.That(data.Matches.Select(m => m.ToString()), Is.EqualTo(compiled.Matches.Select(m => m.Rule)));
            Assert.That(data.Traits.Select(t => t.ToString()), Is.EqualTo(compiled.Traits.Select(t => t.Rule)));
            Assert.That(data.Places.Select(p => p.ToString()), Is.EqualTo(compiled.Places.Select(p => p.Rule)));
        });
    }

    [Test]
    public void Each_trait_of_the_definition_API_has_a_form_as_data() {
        var traits = typeof(Trait).Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(Trait)) && t.GetConstructor(Type.EmptyTypes) is not null)
            .Select(t => (Trait)Activator.CreateInstance(t)!);

        Assert.That(traits.Select(t => TraitExport.Of(t).Text), Is.EqualTo(traits.Select(t => t.ToString())));
    }

    [Test]
    public void The_block_file_has_the_severities_of_the_config() {
        var state = Data.StateFolder.Of(Path.GetTempPath());
        var config = new Dictionary<string, string> { ["umpire.ump020101.severity"] = "warning", ["umpire.max"] = "3", ["Umpire.Check.Mode"] = "warn" };

        var settings = BlockExport.Of(state, new BlockInput(Path.GetTempPath(), ["App"]) {
            InnerBlocks = [Path.Combine(Path.GetTempPath(), "Inner")],
            Config = config,
        });

        Assert.Multiple(() => {
            Assert.That(settings.SeverityOf("UMP020101", Data.Severity.Error), Is.EqualTo(Data.Severity.Warning));
            Assert.That(settings.Mode, Is.EqualTo(Data.CheckMode.Warn));
            Assert.That(settings.Severities, Has.Count.EqualTo(1));
            Assert.That(settings.InnerBlocks, Is.EqualTo(new[] { "Inner" }));
            Assert.That(state.BlockOf(settings), Is.EqualTo(Data.BlockPath.Full(Path.GetTempPath())));
        });
    }

    [Test]
    public void The_definition_sources_have_each_file_of_the_definition_folder_and_are_unknown_after_the_DLL() {
        using var temp = new TempState();
        var dll = typeof(EveryFormArchitecture).Assembly.Location;
        var file = Path.Combine(temp.Block, "EveryForm.cs");
        File.WriteAllText(file, "class A;");
        var sources = new Dictionary<string, string> { [typeof(EveryFormArchitecture).FullName!] = file };
        File.SetLastWriteTimeUtc(file, File.GetLastWriteTimeUtc(dll).AddHours(-1));
        var other = Path.Combine(temp.Block, "Helper.cs");
        File.WriteAllText(other, "class B;");
        sources["Helper"] = other;
        var resolved = new ResolvedArchitecture(new EveryFormArchitecture());

        var marks = DefinitionSources.Of(resolved, temp.Block, sources);
        File.SetLastWriteTimeUtc(file, File.GetLastWriteTimeUtc(dll).AddHours(1));
        var late = DefinitionSources.Of(resolved, temp.Block, sources);

        Assert.Multiple(() => {
            Assert.That(marks.Select(m => m.Path), Is.SupersetOf(new[] { "EveryForm.cs", "Helper.cs" }));
            Assert.That(marks.All(m => m.IsCurrent(temp.Block)), Is.True);
            Assert.That(late.Single(m => m.Path == "EveryForm.cs").IsCurrent(temp.Block), Is.False);
        });
    }

    static string Text(Decision decision) =>
        $"{decision.Permission} by {decision.Rule?.Text} at {decision.Rule?.Access}";

    static string Text(Data.Decision decision) => $"{decision.Permission} by {decision.Rule} at {decision.Access}";

    static IEnumerable<Scenario> Scenarios(Part from, Part to) {
        var fromValues = from.Groups.ToDictionary(g => g.Name, _ => "a");
        return Values([.. to.Groups]).Select(toValues => new Scenario(fromValues, toValues));
    }

    static IEnumerable<Dictionary<string, string>> Values(IReadOnlyList<Part> groups) {
        IEnumerable<Dictionary<string, string>> all = [[]];
        foreach (var group in groups) {
            all = all.SelectMany(values => new[] { "a", "b" }.Select(v => new Dictionary<string, string>(values) { [group.Name] = v }));
        }
        return all;
    }

    sealed record Scenario(Dictionary<string, string> From, Dictionary<string, string> To);
}
