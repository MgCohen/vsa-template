using VSA.Architecture;

namespace Architecture.Tests;

public record Store : Concept;

public record Reader : Concept;

public sealed class DataConceptDefinition(ConceptData? store) : ArchitectureDefinition {
    protected override void Define(Root root) {
        root.Add(store is null ? new Store() : new DataConcept(new Store(), store));
        root.Add<Reader>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<Reader>().Uses<Store>();
    }
}

public sealed class DataConceptTests {
    [Test]
    public void A_data_concept_with_no_override_resolves_as_its_inner_concept() {
        var plain = new DataConceptDefinition(null).Resolve().Select(p => $"{p.From.Path} {p.To.Path} {p.Permission}");
        var wrapped = new DataConceptDefinition(new ConceptData()).Resolve().Select(p => $"{p.From.Path} {p.To.Path} {p.Permission}");

        Assert.That(wrapped, Is.EqualTo(plain));
    }

    [Test]
    public void A_data_concept_matches_the_rules_of_its_inner_concept_and_keeps_its_name() {
        var store = new DataConceptDefinition(new ConceptData(Folder: "Data")).Parts().Single(p => p.Is(typeof(Store)));

        Assert.That(store.Name, Is.EqualTo("Store"));
    }

    [Test]
    public void A_data_concept_folder_overrides_the_folder_of_its_inner_concept() {
        var pairs = new DataConceptDefinition(new ConceptData(Folder: "Data")).Resolve();

        Assert.That(pairs.Any(p => p.From.Path == "Reader" && p.To.Path == "Data" && p.Permission == Permission.Allow), Is.True);
    }

    [Test]
    public void A_data_concept_depth_overrides_the_depth_of_its_inner_concept() {
        var store = new DataConceptDefinition(new ConceptData(Depth: Depth.WithChildren)).Parts().Single(p => p.Is(typeof(Store)));

        Assert.That(store.Depth, Is.EqualTo(Depth.WithChildren));
    }
}
