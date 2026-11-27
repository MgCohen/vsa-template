namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class PlaceTests {
    const string UmpireAssembly = "Umpire.Sample";

    static Definition WithUmpireLibraries => Samples.Layers with {
        Parts = [.. Samples.Layers.Parts, Samples.Part(Samples.Concept("UmpireLibraries"), "UmpireLibraries") with { External = "Umpire" }],
    };

    [Test]
    public void An_in_block_type_of_an_Umpire_assembly_binds_to_its_folder_part() {
        using var temp = new TempState();

        var app = temp.Load(WithUmpireLibraries, Samples.File("Ui/Screen.cs", Type("Screen")));

        Assert.That(app.Find(Name("Screen"))?.Placement?.Part.Name, Is.EqualTo("Ui"));
    }

    [Test]
    public void A_denied_use_between_in_block_types_of_an_Umpire_assembly_is_a_finding() {
        using var temp = new TempState();
        var report = new Report();
        var app = temp.Load(WithUmpireLibraries,
            Samples.File("Ui/Screen.cs", Type("Screen")),
            Samples.File("Data/Store.cs", Type("Store", Use("Screen", 12))));

        ReferenceCheck.Run(app, app.Types, report);

        Assert.That(report.Findings.Select(f => f.Code), Is.EqualTo(new[] { RuleCodes.DeniedUse }));
        Assert.That(report.Findings.Single().Text, Does.StartWith("Store in Data (Data) uses Screen in Ui (Ui)"));
    }

    [Test]
    public void A_type_of_an_Umpire_package_outside_the_block_binds_to_the_External() {
        using var temp = new TempState();
        var app = temp.Load(WithUmpireLibraries, Samples.File("Ui/Screen.cs", Type("Screen")));

        var target = app.TargetOf(new TypeName("Umpire.Config.UmpireConfig", "Umpire.Config"));

        Assert.That(target?.Placement.Part.Name, Is.EqualTo("UmpireLibraries"));
    }

    static TypeName Name(string name) => new($"{UmpireAssembly}.{name}", UmpireAssembly);

    static TypeFacts Type(string name, params ReferenceFact[] references) =>
        Samples.Type(name, references) with { Name = Name(name) };

    static ReferenceFact Use(string name, int line) => Samples.Use(name, line) with { Target = Name(name) };
}
