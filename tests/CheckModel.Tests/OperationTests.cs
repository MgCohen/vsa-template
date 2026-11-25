namespace CheckModel.Tests;

using Umpire.CheckModel;

public sealed class OperationTests {
    [Test]
    public void A_use_that_no_rule_allows_is_a_finding_at_the_line_of_the_use() {
        using var temp = new TempState();
        var report = new Report();
        var app = temp.Load(Samples.Layers,
            Samples.File("Ui/Screen.cs", Samples.Type("Screen")),
            Samples.File("Data/Store.cs", Samples.Type("Store", Samples.Use("Screen", 12))));

        ReferenceCheck.Run(app, app.Types, report);

        var file = Path.Combine(temp.Block, "Data", "Store.cs");
        Assert.That(report.Lines, Is.EqualTo(new[] {
            $"{file}(12,9): error UMP020101: Store in Data (Data) uses Screen in Ui (Ui): no rule allows it "
            + "(DefaultPermission = Deny). Remove the use, or change the rule in Rules.",
        }));
    }

    [Test]
    public void An_operation_checks_only_the_given_types() {
        using var temp = new TempState();
        var report = new Report();
        var app = temp.Load(Samples.Layers,
            Samples.File("Ui/Screen.cs", Samples.Type("Screen")),
            Samples.File("Data/Store.cs", Samples.Type("Store", Samples.Use("Screen", 12))));

        ReferenceCheck.Run(app, [app.Find(Samples.Name("Screen"))!], report);

        Assert.That(report.Findings, Is.Empty);
    }

    [Test]
    public void A_type_in_a_folder_with_no_part_is_a_finding() {
        using var temp = new TempState();
        var report = new Report();
        var app = temp.Load(Samples.Layers, Samples.File("Tools/Clock.cs", Samples.Type("Clock")));

        LocationCheck.Run(app, app.Types, report);

        Assert.That(report.Findings.Single().Code, Is.EqualTo("UMP020202"));
    }

    [Test]
    public void A_severity_of_the_block_replaces_the_severity_of_the_rule() {
        using var temp = new TempState();
        var report = new Report();
        temp.State.Save(Samples.Settings("Sample") with {
            Severities = new Dictionary<string, string> { ["UMP020202"] = "warning" },
        });
        temp.State.Save(Snapshot.Of("Sample", [Samples.File("Tools/Clock.cs", Samples.Type("Clock"))]));
        var app = AppModel.Load(Samples.Layers, temp.State);

        LocationCheck.Run(app, app.Types, report);

        Assert.That(report.Findings.Single().Severity, Is.EqualTo(Severity.Warning));
    }

    [Test]
    public void A_type_with_no_trait_of_its_part_is_a_finding() {
        using var temp = new TempState();
        var report = new Report();
        var isStatic = new Trait(TraitKind.Static, []);
        var definition = Samples.Layers with {
            Traits = [new TraitRule([Samples.Entry(Samples.Ui)], [0], isStatic)],
        };
        var app = temp.Load(definition, Samples.File("Ui/Screen.cs", Samples.Type("Screen")));

        ContentCheck.Run(app, app.Types, report);

        Assert.That(report.Findings.Single().Text, Does.Contain("it is not static, so it breaks the rule \"Ui types are static\""));
    }

    [Test]
    public void The_immutable_trait_reads_a_nested_type_and_names_the_members() {
        using var temp = new TempState();
        var report = new Report();
        var definition = Samples.Layers with {
            Traits = [new TraitRule([Samples.Entry(Samples.Ui)], [0], new Trait(TraitKind.Immutable, []))],
        };
        var plain = Samples.Type("Screen+Part");
        var nested = plain with {
            Location = plain.Location with { DeclaringType = "Sample.Screen" },
            Content = plain.Content with { MutableMembers = ["first", "Second"] },
        };
        var app = temp.Load(definition, Samples.File("Ui/Screen.cs", Samples.Type("Screen"), nested));

        ContentCheck.Run(app, app.Types, report);

        Assert.That(report.Findings.Single().Text, Does.Contain("Part is in Ui (Ui), but it is not immutable (first, Second can change)"));
    }

    [Test]
    public void A_report_with_findings_succeeded_and_a_failed_report_has_a_reason() {
        var found = new Report();
        found.Add(new Finding("UMP020101", Severity.Error, "text"));
        var failed = new Report();
        failed.Fail("the definition is missing");

        Assert.Multiple(() => {
            Assert.That(found.Succeeded, Is.True);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Reason, Is.EqualTo("the definition is missing"));
        });
    }
}
