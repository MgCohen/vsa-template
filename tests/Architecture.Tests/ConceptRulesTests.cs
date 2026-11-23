namespace EngineCore.Tests;

using Umpire.Architecture;
using Umpire.Architecture.Blocks;

public record Room : Block {
    protected override void Define(Part part) {
        part.Add<User>();
    }

    protected override void Rules(RuleBook rules) {
        rules.For<User>().Uses<Target>();
    }
}

public record LostRoom : Block {
    protected override void Rules(RuleBook rules) {
        rules.For<Package>().Uses<Target>();
    }
}

public record StrayRoom : Block {
    protected override void Rules(RuleBook rules) {
        rules.For<Target>().Uses<User>();
    }
}

public sealed class StrayRoomArchitecture : Architecture {
    protected override void Define(Part block) {
        block.Add<Target>();
        block.Add<User>();
        block.AddGroup<StrayRoom>();
    }
}

public sealed class RoomArchitecture(bool denyInRooms) : Architecture {
    public Permission Decision(string fromPath, string toPath) {
        var resolved = new ResolvedArchitecture(this);
        return resolved.UseRules.LastOrDefault(p => p.From.Path == fromPath && p.To.Path == toPath)?.Permission ?? resolved.Options.DefaultPermission!.Value;
    }

    protected override void Define(Part block) {
        block.Add<Target>();
        block.Add<User>();
        block.AddGroup<Room>();
    }

    protected override void Rules(RuleBook rules) {
        if (denyInRooms) {
            rules.For<User>().Under<Room>().Denies<Target>();
        }
    }
}

public sealed class LostRoomArchitecture : Architecture {
    protected override void Define(Part block) {
        block.Add<Target>();
        block.Add<LostRoom>();
    }
}

public sealed class ConceptRulesTests {
    [Test]
    public void A_concept_rule_holds_for_the_parts_of_its_concept() {
        var definition = new RoomArchitecture(false);

        Assert.That(definition.Decision("Room/{room}/User", "Target"), Is.EqualTo(Permission.Allow));
    }

    [Test]
    public void A_concept_rule_does_not_hold_outside_its_concept() {
        var definition = new RoomArchitecture(false);

        Assert.That(definition.Decision("User", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_rule_of_the_architecture_wins_over_a_concept_rule() {
        var definition = new RoomArchitecture(true);

        Assert.That(definition.Decision("Room/{room}/User", "Target"), Is.EqualTo(Permission.Deny));
    }

    [Test]
    public void A_concept_rule_that_names_a_concept_with_no_part_fails() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new LostRoomArchitecture()));

        Assert.That(error!.Message, Does.Contain("For<Package>()"));
    }

    [Test]
    public void A_concept_rule_about_a_part_outside_its_concept_names_the_implicit_filter() {
        var error = Assert.Throws<InvalidOperationException>(() => _ = new ResolvedArchitecture(new StrayRoomArchitecture()));

        Assert.That(error!.Message, Does.Contain("implicit Under<StrayRoom>()"));
    }
}
