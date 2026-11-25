namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class AppModelTests {
    [Test]
    public void A_type_gets_its_part_from_its_folder() {
        using var temp = new TempState();
        var app = temp.Load(Samples.Layers, Samples.File("Ui/Screen.cs", Samples.Type("Screen")));

        Assert.That(app.PlacementOf(app.Types.Single())?.Where, Is.EqualTo("Ui (Ui)"));
    }

    [Test]
    public void A_missing_snapshot_is_a_problem_and_does_not_stop_the_load() {
        using var temp = new TempState();
        temp.State.Save(Samples.Settings("Sample", "Missing"));
        temp.State.Save(Snapshot.Of("Sample", []));

        var app = AppModel.Load(Samples.Layers, temp.State);

        Assert.That(app.Problems.Single().Project, Is.EqualTo("Missing"));
    }

    [TestCase("{")]
    [TestCase("{\"format\":2}")]
    public void A_bad_snapshot_is_a_problem_and_does_not_stop_the_load(string text) {
        using var temp = new TempState();
        temp.State.Save(Samples.Settings("Sample"));
        temp.State.Save(Snapshot.Of("Sample", []));
        File.WriteAllText(temp.State.SnapshotFile("Sample"), text);

        Assert.That(AppModel.Load(Samples.Layers, temp.State).Problems, Has.Count.EqualTo(1));
    }

    [Test]
    public void The_snapshot_of_the_caller_replaces_the_saved_one() {
        using var temp = new TempState();
        temp.Load(Samples.Layers, Samples.File("Ui/Old.cs", Samples.Type("Old")));
        var current = Snapshot.Of("Sample", [Samples.File("Ui/New.cs", Samples.Type("New"))]);

        var app = AppModel.Load(Samples.Layers, temp.State, new CurrentProject(current, _ => []));

        Assert.That(app.Types.Select(t => t.Name.Name), Is.EqualTo(new[] { "New" }));
    }

    [Test]
    public void A_partial_type_is_one_type_with_the_facts_of_each_file() {
        using var temp = new TempState();
        var app = temp.Load(Samples.Layers,
            Samples.File("Ui/Screen.cs", Samples.Type("Screen", Samples.Use("First", 5))),
            Samples.File("Ui/Screen.Part.cs", Samples.Type("Screen", Samples.Use("Second", 6))));

        var uses = app.Types.Single().References;
        Assert.That(uses.Select(r => r.Fact.Target.Name), Is.EquivalentTo(new[] { "First", "Second" }));
        Assert.That(uses.Select(r => Path.GetFileName(r.File)), Is.EquivalentTo(new[] { "Screen.cs", "Screen.Part.cs" }));
    }
}
